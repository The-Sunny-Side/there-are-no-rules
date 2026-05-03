using System.Collections.Generic;
using PurrNet;
using UnityEngine;

[RequireComponent(typeof(AIInputProvider))]
public class AIDriver : MonoBehaviour
{
    [Header("Road")]
    [Tooltip("Sequenza di road da percorrere in ordine. Il bot passa alla successiva quando si avvicina alla fine della corrente. Viene sovrascritta da Initialize() se chiamato (es. dal BotSpawner).")]
    [SerializeField] private List<RoadMeshGenerator> roadSequence = new();
    [Tooltip("Numero di punti spline dalla fine entro cui il bot passa alla road successiva.")]
    [SerializeField] private int advanceThresholdFromEnd = 3;

    [Header("Pure Pursuit")]
    [Tooltip("Distanza in avanti sulla spline a cui puntare. Più alta = curve più larghe, meno oscillazione.")]
    [SerializeField] private float lookaheadDistance = 6f;
    [Tooltip("Fattore che moltiplica la distanza di lookahead in base alla velocità del rigidbody.")]
    [SerializeField] private float lookaheadSpeedFactor = 0.3f;
    [Tooltip("Tetto massimo del lookahead. Se vuoi puntare più lontano alza ANCHE questo valore, non solo lookaheadDistance.")]
    [SerializeField] private float minLookahead = 3f;
    [SerializeField] private float maxLookahead = 60f;
    [Tooltip("Moltiplicatore dello steer prima del clamp. >1 = sterzata più aggressiva (satura prima per offset moderati).")]
    [SerializeField] private float steerAggression = 2.5f;

    [Header("Variation")]
    [Tooltip("Se attivo, applica variazioni continue per evitare che tutti i bot guidino uguale.")]
    [SerializeField] private bool enableNoise = true;
    [Tooltip("Intensita' generale delle variazioni. 0 = guida pulita, 1 = differenza evidente.")]
    [Range(0f, 1f)]
    [SerializeField] private float variationStrength = 0.35f;
    [Tooltip("Quanto il bot varia la traiettoria e il punto verso cui punta.")]
    [Range(0f, 1f)]
    [SerializeField] private float lineVariation = 0.5f;
    [Tooltip("Quanto il bot varia il ritmo e il gas in uscita di curva.")]
    [Range(0f, 1f)]
    [SerializeField] private float paceVariation = 0.25f;
    [Tooltip("Quanto rapidamente cambiano queste variazioni nel tempo.")]
    [Range(0.25f, 2f)]
    [SerializeField] private float variationSpeed = 1f;
    [Tooltip("Seed opzionale. Valore negativo = seed ricavato automaticamente dal bot.")]
    [SerializeField] private float noiseSeed = -1f;

    [Header("Throttle")]
    [Range(0f, 1f)]
    [SerializeField] private float throttle = 1f;
    [Tooltip("Frazione di throttle a cui scendere quando lo steer è saturato. 1 = nessuna riduzione, 0.7 = -30% in piena sterzata.")]
    [Range(0f, 1f)]
    [SerializeField] private float throttleAtFullSteer = 0.7f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private AIInputProvider _input;
    private Rigidbody _rb;
    private int _roadIndex;
    private int _closestIndex;
    private Vector3 _debugTarget;
    private float _debugLookahead;
    private float _debugLateralOffset;
    private float _debugThrottle;
    private float _resolvedNoiseSeed;

    void Awake()
    {
        _input = GetComponent<AIInputProvider>();
        _rb = GetComponent<Rigidbody>();
        ResolveNoiseSeed();
    }

    void FixedUpdate()
    {
        if (!IsServer()) return;
        if (LobbyState.Instance != null && !LobbyState.Instance.IsRaceActive)
        {
            if (_input != null)
            {
                _input.Steer = 0f;
                _input.Throttle = 0f;
                _input.JumpTapped = false;
            }
            return;
        }
        if (roadSequence == null || roadSequence.Count == 0) return;

        RoadMeshGenerator road = GetCurrentRoad();
        if (road == null || !road.IsGenerated) return;

        var points = road.SplinePoints;
        _closestIndex = FindClosestIndex(transform.position, _closestIndex, points);

        if (ShouldAdvanceRoad(points.Count) && TryAdvanceRoad())
        {
            road = GetCurrentRoad();
            if (road == null || !road.IsGenerated) return;
            points = road.SplinePoints;
        }

        float speed = _rb != null ? _rb.linearVelocity.magnitude : 0f;
        float baseLookahead = Mathf.Clamp(lookaheadDistance + speed * lookaheadSpeedFactor, minLookahead, maxLookahead);
        Vector3 target = GetLookaheadPoint(_closestIndex, baseLookahead, points);
        float lookahead = baseLookahead;
        float lateralOffset = 0f;
        float steerBias = 0f;
        float throttleNoise = 0f;

        if (enableNoise && variationStrength > 0.0001f)
        {
            float noiseStrength = ExpandSlider(variationStrength);
            float lineStrength = noiseStrength * ExpandSlider(lineVariation);
            float paceStrength = noiseStrength * ExpandSlider(paceVariation);

            float lookaheadAmplitude = Mathf.Max(4f, lookaheadDistance * 0.65f) * lineStrength;
            float lookaheadNoise = SampleSignedNoise(_resolvedNoiseSeed + 3.17f, 0.45f * variationSpeed) * lookaheadAmplitude;
            lookahead = Mathf.Clamp(baseLookahead + lookaheadNoise, minLookahead, maxLookahead);
            target = GetLookaheadPoint(_closestIndex, lookahead, points);

            float lateralAmplitude = 4.5f * lineStrength;
            lateralOffset = SampleSignedNoise(_resolvedNoiseSeed + 9.41f, 0.28f * variationSpeed) * lateralAmplitude;
            if (Mathf.Abs(lateralOffset) > 0.0001f)
                target += GetPathRight(_closestIndex, points) * lateralOffset;

            steerBias = SampleSignedNoise(_resolvedNoiseSeed + 12.61f, 0.38f * variationSpeed) * (0.45f * lineStrength);
            throttleNoise = SampleSignedNoise(_resolvedNoiseSeed + 15.73f, 0.55f * variationSpeed) * (0.4f * paceStrength);
        }

        _debugTarget = target;
        _debugLookahead = lookahead;
        _debugLateralOffset = lateralOffset;

        Vector3 local = transform.InverseTransformPoint(target);
        float mag = new Vector2(local.x, local.z).magnitude;
        float steer = mag > 0.0001f ? Mathf.Clamp(local.x / mag * steerAggression, -1f, 1f) : 0f;
        steer = Mathf.Clamp(steer + steerBias, -1f, 1f);

        float throttleScale = Mathf.Lerp(1f, throttleAtFullSteer, Mathf.Abs(steer));
        float finalThrottle = Mathf.Clamp01(throttle * throttleScale * (1f + throttleNoise));

        _input.Steer = steer;
        _input.Throttle = finalThrottle;
        _input.JumpTapped = false;
        _debugThrottle = finalThrottle;
    }

    private static bool IsServer()
    {
        return NetworkManager.main != null && NetworkManager.main.isServer;
    }

    // Iniettata dal BotSpawner per i bot creati a runtime, dato che i RoadMeshGenerator sono istanze di scena
    // e non possono essere referenziate dal prefab.
    public void Initialize(IEnumerable<RoadMeshGenerator> sequence)
    {
        roadSequence.Clear();
        if (sequence != null)
            roadSequence.AddRange(sequence);
        ResolveNoiseSeed();
        _roadIndex = 0;
        _closestIndex = 0;
        ResetTracking();
    }

    private RoadMeshGenerator GetCurrentRoad()
    {
        if (_roadIndex < 0 || _roadIndex >= roadSequence.Count) return null;
        return roadSequence[_roadIndex];
    }

    // Trova la road + punto spline più vicino globalmente. Da chiamare dopo un teleport/respawn.
    public void ResetTracking()
    {
        if (roadSequence == null || roadSequence.Count == 0) return;

        Vector3 pos = transform.position;
        int bestRoad = -1;
        int bestPoint = 0;
        float bestSqr = float.MaxValue;

        for (int r = 0; r < roadSequence.Count; r++)
        {
            var road = roadSequence[r];
            if (road == null || !road.IsGenerated) continue;

            var points = road.SplinePoints;
            for (int i = 0; i < points.Count; i++)
            {
                float sqr = (points[i] - pos).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    bestRoad = r;
                    bestPoint = i;
                }
            }
        }

        if (bestRoad >= 0)
        {
            _roadIndex = bestRoad;
            _closestIndex = bestPoint;
        }
    }

    private bool ShouldAdvanceRoad(int pointCount)
    {
        if (_roadIndex >= roadSequence.Count - 1) return false;
        return _closestIndex >= pointCount - advanceThresholdFromEnd;
    }

    private bool TryAdvanceRoad()
    {
        int nextIndex = _roadIndex + 1;
        if (nextIndex >= roadSequence.Count) return false;

        var nextRoad = roadSequence[nextIndex];
        if (nextRoad == null || !nextRoad.IsGenerated) return false;

        _roadIndex = nextIndex;
        _closestIndex = FindClosestIndexFullScan(transform.position, nextRoad.SplinePoints);
        return true;
    }

    private static int FindClosestIndex(Vector3 worldPos, int startIndex, IReadOnlyList<Vector3> points)
    {
        int count = points.Count;
        startIndex = Mathf.Clamp(startIndex, 0, count - 1);

        const int lookBack = 5;
        const int lookForward = 40;
        int from = Mathf.Max(0, startIndex - lookBack);
        int to = Mathf.Min(count - 1, startIndex + lookForward);

        int best = startIndex;
        float bestSqr = float.MaxValue;
        for (int i = from; i <= to; i++)
        {
            float sqr = (points[i] - worldPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = i;
            }
        }
        return best;
    }

    private static int FindClosestIndexFullScan(Vector3 worldPos, IReadOnlyList<Vector3> points)
    {
        int best = 0;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float sqr = (points[i] - worldPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = i;
            }
        }
        return best;
    }

    private static Vector3 GetLookaheadPoint(int startIndex, float distance, IReadOnlyList<Vector3> points)
    {
        int count = points.Count;
        if (startIndex >= count - 1) return points[count - 1];

        float remaining = distance;
        for (int i = startIndex; i < count - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            float segLen = Vector3.Distance(a, b);
            if (segLen >= remaining)
            {
                float t = remaining / segLen;
                return Vector3.Lerp(a, b, t);
            }
            remaining -= segLen;
        }
        return points[count - 1];
    }

    private static Vector3 GetPathRight(int startIndex, IReadOnlyList<Vector3> points)
    {
        int count = points.Count;
        if (count < 2) return Vector3.right;

        int from = Mathf.Clamp(startIndex, 0, count - 2);
        Vector3 tangent = (points[from + 1] - points[from]).normalized;
        if (tangent.sqrMagnitude < 0.0001f) return Vector3.right;

        Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
        return right.sqrMagnitude > 0.0001f ? right : Vector3.right;
    }

    private void ResolveNoiseSeed()
    {
        if (noiseSeed >= 0f)
        {
            _resolvedNoiseSeed = noiseSeed;
            return;
        }

        var identity = GetComponent<PlayerIdentity>();
        int uniqueId = identity != null && identity.BotRaceId > 0
            ? identity.BotRaceId
            : Mathf.Abs(gameObject.GetInstanceID());

        _resolvedNoiseSeed = uniqueId * 0.6180339f + 17.123f;
    }

    private float SampleSignedNoise(float seed, float frequency)
    {
        float time = Time.time * Mathf.Max(0.01f, frequency);
        float primary = Mathf.PerlinNoise(seed, time) * 2f - 1f;
        float secondary = Mathf.PerlinNoise(seed * 1.73f + 11f, time * 1.91f + 7f) * 2f - 1f;
        return Mathf.Clamp(primary * 0.7f + secondary * 0.3f, -1f, 1f);
    }

    private static float ExpandSlider(float value)
    {
        value = Mathf.Clamp01(value);
        return 1f - Mathf.Pow(1f - value, 2.2f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        var road = GetCurrentRoad();
        if (road == null || !road.IsGenerated) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(_debugTarget, 0.4f);
        Gizmos.DrawLine(transform.position, _debugTarget);

        float realDistance = Vector3.Distance(transform.position, _debugTarget);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            $"lookahead computed: {_debugLookahead:F1}m\nlateral offset: {_debugLateralOffset:F2}m\nthrottle: {_debugThrottle:F2}\nbot-target dist: {realDistance:F1}m");
    }
#endif
}
