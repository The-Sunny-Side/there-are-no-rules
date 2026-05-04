using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PurrNet;
using PurrNet.Packing;
using UnityEngine;

// Server-auth. Concatena tutti i RoadMeshGenerator in UNA SOLA spline (array flat di Vector3).
// Per ogni PlayerIdentity nella scena proietta la posizione su questa spline e ottiene un singolo
// indice intero -> arc length cumulativa = progresso scalare.
// Sort discendente -> rank. Replica via SyncDictionary<RacerKey,int>.
public class RaceStandings : NetworkBehaviour
{
    public static RaceStandings Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private List<RoadMeshGenerator> roadSequence = new();

    [Header("Settings")]
    [Tooltip("Frequenza di ricalcolo dei rank sul server (secondi). 0.1 = 10Hz.")]
    [SerializeField] private float updateRate = 0.1f;
    [Tooltip("Indici INDIETRO da scansionare attorno alla posizione precedente.")]
    [SerializeField] private int lookBack = 5;
    [Tooltip("Indici AVANTI da scansionare attorno alla posizione precedente. Tieni largo: copre i salti del veloce.")]
    [SerializeField] private int lookForward = 100;
    [Tooltip("Distanza max (m) entro cui consideriamo valida la proiezione locale. Oltre, fa fallback a scan globale.")]
    [SerializeField] private float fallbackDistance = 15f;
    [Tooltip("Stampa il ranking per-tick sul server (abilitalo per debug).")]
    [SerializeField] private bool verboseDebug = false;

    private readonly SyncDictionary<RacerKey, int> _ranks = new();

    // Server-only
    private Vector3[] _allPoints;
    private float[] _cumLen;
    private readonly Dictionary<RacerKey, int> _projection = new(); // racer -> indice corrente in _allPoints
    private readonly List<(RacerKey key, float progress, int idx)> _scratch = new();
    private readonly HashSet<RacerKey> _seenKeys = new();
    private readonly List<RacerKey> _staleKeys = new();
    private Coroutine _tickCo;

    public int TotalRacers => isSpawned ? _ranks.Count : 0;

    public int LocalRank
    {
        get
        {
            if (!isSpawned) return 0;
            if (!localPlayer.HasValue) return 0;
            return _ranks.TryGetValue(RacerKey.FromPlayer(localPlayer.Value), out var r) ? r : 0;
        }
    }

    // Debug: log locale ad ogni cambio di rank/totale.
    private int _lastLoggedRank = -1;
    private int _lastLoggedTotal = -1;
    void Update()
    {
        if (!isSpawned) return;
        int rank = LocalRank;
        int total = TotalRacers;
        if (rank == _lastLoggedRank && total == _lastLoggedTotal) return;
        _lastLoggedRank = rank;
        _lastLoggedTotal = total;
        Debug.Log($"[RaceStandings] LocalRank={rank} TotalRacers={total}");
    }

    void Awake()
    {
        Instance = this;
    }

    private new void OnDestroy()
    {
        if (_tickCo != null) StopCoroutine(_tickCo);
        if (Instance == this) Instance = null;
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        if (!isServer) return;
        BuildFlatSpline();
        if (verboseDebug)
            Debug.Log($"[RaceStandings] Built flat spline: {_allPoints?.Length ?? 0} points, total length {(_cumLen != null && _cumLen.Length > 0 ? _cumLen[_cumLen.Length - 1] : 0):F1}m");
        _tickCo = StartCoroutine(TickRoutine());
    }

    // Concatena tutte le SplinePoints in un singolo array, dedup-ando i punti
    // di confine (se l'ultimo punto del road r coincide col primo del road r+1).
    private void BuildFlatSpline()
    {
        var points = new List<Vector3>();
        for (int r = 0; r < roadSequence.Count; r++)
        {
            var road = roadSequence[r];
            if (road == null || !road.IsGenerated) continue;
            var pts = road.SplinePoints;
            if (pts == null || pts.Count == 0) continue;

            int from = 0;
            if (points.Count > 0 && (points[points.Count - 1] - pts[0]).sqrMagnitude < 0.0001f)
                from = 1; // dedup boundary
            for (int i = from; i < pts.Count; i++)
                points.Add(pts[i]);
        }

        _allPoints = points.ToArray();
        _cumLen = new float[_allPoints.Length];
        if (_allPoints.Length == 0) return;
        _cumLen[0] = 0;
        for (int i = 1; i < _allPoints.Length; i++)
            _cumLen[i] = _cumLen[i - 1] + Vector3.Distance(_allPoints[i - 1], _allPoints[i]);
    }

    private IEnumerator TickRoutine()
    {
        var wait = new WaitForSeconds(updateRate);
        while (true)
        {
            if (LobbyState.Instance != null && LobbyState.Instance.IsRaceActive)
                ComputeRanks();
            yield return wait;
        }
    }

    private void ComputeRanks()
    {
        if (_allPoints == null || _allPoints.Length == 0) return;

        _scratch.Clear();
        _seenKeys.Clear();

        var identities = FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        for (int i = 0; i < identities.Length; i++)
        {
            var id = identities[i];
            if (id == null) continue;
            if (!TryGetRacerKey(id, out var key)) continue;
            _seenKeys.Add(key);

            Vector3 pos = id.transform.position;

            int idx;
            float bestDistSqr;
            if (_projection.TryGetValue(key, out var prev))
            {
                (idx, bestDistSqr) = ProjectLocal(pos, prev);
                // Se la proiezione locale è troppo distante, qualcosa non va: rifai globale.
                if (bestDistSqr > fallbackDistance * fallbackDistance)
                {
                    if (verboseDebug)
                        Debug.LogWarning($"[RaceStandings] {KeyName(key)}: local proj distance {Mathf.Sqrt(bestDistSqr):F1}m > {fallbackDistance}m, fallback global");
                    (idx, _) = ProjectGlobal(pos);
                }
                // Monotonic: il progresso non torna mai indietro (evita flicker da movimento laterale).
                if (idx < prev) idx = prev;
            }
            else
            {
                (idx, _) = ProjectGlobal(pos);
            }
            _projection[key] = idx;

            float progress = _cumLen[idx];
            _scratch.Add((key, progress, idx));
        }

        // Cleanup despawned racer
        _staleKeys.Clear();
        foreach (var k in _projection.Keys)
            if (!_seenKeys.Contains(k)) _staleKeys.Add(k);
        for (int i = 0; i < _staleKeys.Count; i++)
        {
            _projection.Remove(_staleKeys[i]);
            if (_ranks.ContainsKey(_staleKeys[i])) _ranks.Remove(_staleKeys[i]);
        }

        // Sort desc per progress; tie-break deterministico.
        _scratch.Sort(static (a, b) =>
        {
            int c = b.progress.CompareTo(a.progress);
            return c != 0 ? c : a.key.GetHashCode().CompareTo(b.key.GetHashCode());
        });

        for (int i = 0; i < _scratch.Count; i++)
        {
            var k = _scratch[i].key;
            int rank = i + 1;
            if (!_ranks.TryGetValue(k, out var existing) || existing != rank)
                _ranks[k] = rank;
        }

        if (verboseDebug && _scratch.Count > 0)
        {
            var sb = new StringBuilder("[RaceStandings] ");
            for (int i = 0; i < _scratch.Count; i++)
            {
                var s = _scratch[i];
                sb.Append($"#{i + 1} {KeyName(s.key)} idx={s.idx}/{_allPoints.Length - 1} prog={s.progress:F1}m  ");
            }
            Debug.Log(sb.ToString());
        }
    }

    private static bool TryGetRacerKey(PlayerIdentity id, out RacerKey key)
    {
        if (id.IsBot && id.BotRaceId > 0)
        {
            key = RacerKey.FromBot(id.BotRaceId);
            return true;
        }
        if (id.TryGetOwner(out var pid))
        {
            key = RacerKey.FromPlayer(pid);
            return true;
        }
        key = default;
        return false;
    }

    private static string KeyName(RacerKey k) => k.isBot ? $"bot{k.botRaceId}" : $"p{k.playerId}";

    // Scan globale di TUTTI i punti della spline unificata.
    private (int idx, float distSqr) ProjectGlobal(Vector3 pos)
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < _allPoints.Length; i++)
        {
            float d = (pos - _allPoints[i]).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return (best, bestDist);
    }

    // Scan locale ±lookBack/±lookForward attorno all'indice precedente. Restituisce anche distSqr per fallback.
    private (int idx, float distSqr) ProjectLocal(Vector3 pos, int hint)
    {
        int n = _allPoints.Length;
        int from = Mathf.Max(0, hint - lookBack);
        int to = Mathf.Min(n - 1, hint + lookForward);
        int best = Mathf.Clamp(hint, 0, n - 1);
        float bestDist = float.MaxValue;
        for (int i = from; i <= to; i++)
        {
            float d = (pos - _allPoints[i]).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return (best, bestDist);
    }
}

public struct RacerKey : IPackedAuto, IEquatable<RacerKey>
{
    public bool isBot;
    public PlayerID playerId;
    public int botRaceId;

    public static RacerKey FromPlayer(PlayerID id) => new() { isBot = false, playerId = id };
    public static RacerKey FromBot(int botRaceId) => new() { isBot = true, botRaceId = botRaceId };

    public bool Equals(RacerKey other)
        => isBot == other.isBot && (isBot ? botRaceId == other.botRaceId : playerId == other.playerId);

    public override bool Equals(object obj) => obj is RacerKey o && Equals(o);
    public override int GetHashCode() => isBot ? botRaceId : playerId.GetHashCode();
}
