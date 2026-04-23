using System.Collections.Generic;
using PurrNet;
using UnityEngine;

[RequireComponent(typeof(AIInputProvider))]
public class AIDriver : NetworkBehaviour
{
    [Header("Road")]
    [Tooltip("Sequenza di road da percorrere in ordine. Il bot passa alla successiva quando si avvicina alla fine della corrente.")]
    [SerializeField] private List<RoadMeshGenerator> roadSequence = new();
    [Tooltip("Numero di punti spline dalla fine entro cui il bot passa alla road successiva.")]
    [SerializeField] private int advanceThresholdFromEnd = 3;

    [Header("Pure Pursuit")]
    [Tooltip("Distanza in avanti sulla spline a cui puntare. Più alta = curve più larghe, meno oscillazione.")]
    [SerializeField] private float lookaheadDistance = 6f;
    [Tooltip("Fattore che moltiplica la distanza di lookahead in base alla velocità del rigidbody.")]
    [SerializeField] private float lookaheadSpeedFactor = 0.3f;
    [SerializeField] private float minLookahead = 3f;
    [SerializeField] private float maxLookahead = 20f;

    [Header("Throttle (step 1: fisso)")]
    [Range(0f, 1f)]
    [SerializeField] private float throttle = 1f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private AIInputProvider _input;
    private Rigidbody _rb;
    private int _roadIndex;
    private int _closestIndex;
    private Vector3 _debugTarget;

    void Awake()
    {
        _input = GetComponent<AIInputProvider>();
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (!isServer) return;
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
        float lookahead = Mathf.Clamp(lookaheadDistance + speed * lookaheadSpeedFactor, minLookahead, maxLookahead);
        Vector3 target = GetLookaheadPoint(_closestIndex, lookahead, points);
        _debugTarget = target;

        Vector3 local = transform.InverseTransformPoint(target);
        float mag = new Vector2(local.x, local.z).magnitude;
        float steer = mag > 0.0001f ? Mathf.Clamp(local.x / mag, -1f, 1f) : 0f;

        _input.Steer = steer;
        _input.Throttle = throttle;
        _input.JumpTapped = false;
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

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        var road = GetCurrentRoad();
        if (road == null || !road.IsGenerated) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(_debugTarget, 0.4f);
        Gizmos.DrawLine(transform.position, _debugTarget);
    }
#endif
}
