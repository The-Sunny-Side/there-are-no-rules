using System;
using System.Collections;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Packing;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// Server-auth. Concatena tutti i RoadMeshGenerator in UNA SOLA spline (array flat di Vector3).
// Per ogni PlayerIdentity nella scena, ogni updateRate secondi, scan globale dei punti spline ->
// punto più vicino -> arc length cumulativa = progress scalare. Sort desc -> rank.
// Stateless: nessuna proiezione precedente, nessun monotonic, nessun fallback. Sempre corretto.
public class RaceStandings : NetworkBehaviour
{
    public static RaceStandings Instance { get; private set; }
    [Header("UI")] [SerializeField] private TextMeshProUGUI localRankText;

    [Header("Refs")]
    [SerializeField] private List<RoadMeshGenerator> roadSequence = new();

    [Header("Settings")]
    [Tooltip("Frequenza di ricalcolo dei rank sul server (secondi). 0.1 = 10Hz.")]
    [SerializeField] private float updateRate = 0.1f;

    private readonly SyncDictionary<RacerKey, int> _ranks = new();

    // Server-only
    private Vector3[] _allPoints;
    private float[] _cumLen;
    private readonly List<(RacerKey key, float progress)> _scratch = new();
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

    private int _lastLoggedRank = -1;
    private int _lastLoggedTotal = -1;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        // Forza il refresh del testo al prossimo Update anche se rank/total non sono cambiati,
        // così il suffisso ordinale si aggiorna subito se la lingua cambia a metà gara.
        _lastLoggedRank = -1;
    }

    void Update()
    {
        if (!isSpawned) return;
        int rank = LocalRank;
        int total = TotalRacers;
        if (rank == _lastLoggedRank && total == _lastLoggedTotal) return;
        _lastLoggedRank = rank;
        _lastLoggedTotal = total;

        if (rank <= 0)
        {
            localRankText.text = $"-/{total}";
            return;
        }

        string suffix = GetOrdinalSuffix(rank);
        localRankText.text = $"{rank}{suffix}/{total}";
    }

    private static string GetOrdinalSuffix(int n)
    {
        string localeCode = LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en";

        if (localeCode.StartsWith("it", StringComparison.OrdinalIgnoreCase))
            return "°"; // Italiano: stesso indicatore ordinale per ogni numero

        // Fallback inglese (copre anche "en" e qualsiasi locale non gestita)
        int rem100 = n % 100;
        if (rem100 >= 11 && rem100 <= 13) return "<size=60%>th</size>";
        return (n % 10) switch
        {
            1 => "<size=60%>st</size>",
            2 => "<size=60%>nd</size>",
            3 => "<size=60%>rd</size>",
            _ => "<size=60%>th</size>"
        };
    }

    void Awake()
    {
        Instance = this;
    }

    private new void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        if (_tickCo != null) StopCoroutine(_tickCo);
        if (Instance == this) Instance = null;
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        if (localRankText == null)
            Debug.LogWarning("[RaceStandings] 'localRankText' non collegato: il contatore posizione non verrà aggiornato. Collegalo nell'Inspector (PlayScene).", this);
        if (!isServer) return;
        BuildFlatSpline();
        _tickCo = StartCoroutine(TickRoutine());
    }

    // Concatena tutte le SplinePoints in un singolo array, dedup-ando i punti
    // di confine (se l'ultimo punto del road r coincide col primo del road r+1).
    private void BuildFlatSpline()
    {
        if (roadSequence == null || roadSequence.Count == 0)
        {
            Debug.LogWarning("[RaceStandings] 'roadSequence' è vuota: collega i RoadMeshGenerator della mappa nell'Inspector (PlayScene) → senza road il contatore resterà 0/0.", this);
        }

        int validRoads = 0;
        int nullRoads = 0;
        int notGeneratedRoads = 0;

        var points = new List<Vector3>();
        for (int r = 0; r < roadSequence.Count; r++)
        {
            var road = roadSequence[r];
            if (road == null) { nullRoads++; continue; }
            if (!road.IsGenerated) { notGeneratedRoads++; continue; }
            var pts = road.SplinePoints;
            if (pts == null || pts.Count == 0) continue;
            validRoads++;

            int from = 0;
            if (points.Count > 0 && (points[points.Count - 1] - pts[0]).sqrMagnitude < 0.0001f)
                from = 1; // dedup boundary
            for (int i = from; i < pts.Count; i++)
                points.Add(pts[i]);
        }

        _allPoints = points.ToArray();
        _cumLen = new float[_allPoints.Length];
        if (_allPoints.Length == 0)
        {
            Debug.LogWarning($"[RaceStandings] Spline finale vuota → il contatore resterà 0/0. roadSequence={roadSequence.Count} (validi={validRoads}, null/non collegati={nullRoads}, non generati={notGeneratedRoads}). Collega i RoadMeshGenerator della mappa in PlayScene.", this);
            return;
        }
        Debug.Log($"[RaceStandings] Spline costruita: {_allPoints.Length} punti da {validRoads} road.", this);
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

            int idx = FindClosestIndex(id.transform.position);
            _scratch.Add((key, _cumLen[idx]));
        }

        // Cleanup: rimuovi dalle _ranks i racer che non sono più nella scena
        _staleKeys.Clear();
        foreach (var k in _ranks.Keys)
            if (!_seenKeys.Contains(k)) _staleKeys.Add(k);
        for (int i = 0; i < _staleKeys.Count; i++)
            _ranks.Remove(_staleKeys[i]);

        // Sort desc per progress; tie-break deterministico via hash della key.
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

    // Scan globale: il punto più vicino fra tutti quelli della spline unificata.
    private int FindClosestIndex(Vector3 pos)
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < _allPoints.Length; i++)
        {
            float d = (pos - _allPoints[i]).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
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