using System;
using System.Collections;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

// Singleton di scena. Server-auth: gestisce ready states, click Play dell'host, countdown e attivazione gara.
// Stato propagato ai client via ObserversRpc.
public class LobbyState : NetworkBehaviour
{
    public static LobbyState Instance { get; private set; }

    [Header("References")]
    [SerializeField] private NetworkManager netManager;
    [SerializeField] private BotSpawner botSpawner;

    [Header("Settings")]
    [Tooltip("Secondi del countdown 3-2-1 prima dell'avvio della gara.")]
    [SerializeField] private int countdownSeconds = 3;
    [SerializeField] private int maxDisplayNameLength = 24;

    // Verità server. Mirrorata sui client tramite ObserversRpc bufferLast.
    private readonly Dictionary<PlayerID, bool> _readyStates = new();
    private readonly Dictionary<PlayerID, string> _displayNames = new();
    private readonly List<PlayerID> _playerOrder = new();
    // Nomi ricevuti prima che il player sia registrato in _playerOrder.
    private readonly Dictionary<PlayerID, string> _pendingDisplayNames = new();

    public IReadOnlyList<PlayerID> Players => _playerOrder;
    public int PlayerCount => _playerOrder.Count;
    public bool IsReady(PlayerID id) => _readyStates.TryGetValue(id, out var r) && r;
    public string GetDisplayName(PlayerID id) =>
        _displayNames.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : id.ToString();

    public bool IsRaceActive { get; private set; }
    public bool CountdownActive { get; private set; }
    public int CountdownValue { get; private set; }

    public event Action OnLobbyStateChanged;
    public event Action<int> OnCountdownTick;
    public event Action OnRaceStarted;

    void Awake()
    {
        Instance = this;
    }

    private new void OnDestroy()
    {
        if (netManager != null)
        {
            netManager.onPlayerJoined -= HandlePlayerJoined;
            netManager.onPlayerLeft -= HandlePlayerLeft;
        }
        if (Instance == this) Instance = null;
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        Debug.Log($"[LobbyState] OnSpawned. isServer={isServer}, isHost={isHost}");
        if (netManager != null)
        {
            netManager.onPlayerJoined += HandlePlayerJoined;
            netManager.onPlayerLeft += HandlePlayerLeft;
        }
        StartCoroutine(SubmitLocalDisplayNameWhenReady());
    }

    private IEnumerator SubmitLocalDisplayNameWhenReady()
    {
        while (!localPlayer.HasValue)
            yield return null;
        SubmitLocalDisplayName(GameConfig.Data.name);
    }

    // Sul host info.sender di un ServerRpc è l'identità "Server" e non il PlayerID
    // del giocatore locale, quindi bypassiamo l'RPC e applichiamo il nome direttamente.
    private void SubmitLocalDisplayName(string displayName)
    {
        if (!localPlayer.HasValue) return;
        if (isServer)
            ApplyDisplayName(localPlayer.Value, displayName);
        else
            SetDisplayNameServer(displayName);
    }

    private void ApplyDisplayName(PlayerID player, string displayName)
    {
        string normalized = NormalizeDisplayName(displayName, player);

        if (!_playerOrder.Contains(player))
        {
            _pendingDisplayNames[player] = normalized;
            return;
        }

        _displayNames[player] = normalized;
        BroadcastLobbySnapshot(_playerOrder.ToArray(), BuildReadySnapshot(), BuildDisplayNameSnapshot());
    }

    private void HandlePlayerJoined(PlayerID player, bool isReconnect, bool asServer)
    {
        if (!asServer) return;
        if (_playerOrder.Contains(player)) return;
        _playerOrder.Add(player);
        _readyStates[player] = false;

        if (_pendingDisplayNames.TryGetValue(player, out var pendingName))
        {
            _displayNames[player] = pendingName;
            _pendingDisplayNames.Remove(player);
        }
        else if (!_displayNames.ContainsKey(player))
        {
            _displayNames[player] = player.ToString();
        }

        Debug.Log("nome del player da gestire:" + GetDisplayName(player));
        BroadcastLobbySnapshot(_playerOrder.ToArray(), BuildReadySnapshot(), BuildDisplayNameSnapshot());
    }

    private void HandlePlayerLeft(PlayerID player, bool asServer)
    {
        if (!asServer) return;
        _pendingDisplayNames.Remove(player);
        if (!_playerOrder.Remove(player)) return;
        _readyStates.Remove(player);
        _displayNames.Remove(player);
        BroadcastLobbySnapshot(_playerOrder.ToArray(), BuildReadySnapshot(), BuildDisplayNameSnapshot());
    }

    [ObserversRpc(bufferLast: true)]
    private void BroadcastLobbySnapshot(PlayerID[] players, bool[] readyStates, string[] displayNames)
    {
        _playerOrder.Clear();
        _readyStates.Clear();
        _displayNames.Clear();

        int count = players != null ? players.Length : 0;
        for (int i = 0; i < count; i++)
        {
            PlayerID player = players[i];
            _playerOrder.Add(player);

            bool ready = readyStates != null && i < readyStates.Length && readyStates[i];
            _readyStates[player] = ready;

            string displayName = displayNames != null && i < displayNames.Length
                ? displayNames[i]
                : player.ToString();
            _displayNames[player] = NormalizeDisplayName(displayName, player);
        }

        OnLobbyStateChanged?.Invoke();
    }

    [ServerRpc(requireOwnership: false)]
    private void SetDisplayNameServer(string displayName, RPCInfo info = default)
    {
        Debug.Log($"Ricevuto nome '{displayName}' da {info.sender}");
        ApplyDisplayName(info.sender, displayName);
    }

    public void RequestToggleReady()
    {
        ToggleReadyServer();
    }

    [ServerRpc(requireOwnership: false)]
    private void ToggleReadyServer(RPCInfo info = default)
    {
        if (IsRaceActive || CountdownActive) return;
        if (!_readyStates.ContainsKey(info.sender)) return;
        bool newState = !_readyStates[info.sender];
        _readyStates[info.sender] = newState;
        BroadcastLobbySnapshot(_playerOrder.ToArray(), BuildReadySnapshot(), BuildDisplayNameSnapshot());
    }

    public bool AreAllPlayersReady()
    {
        if (_playerOrder.Count == 0) return false;
        foreach (var p in _playerOrder)
            if (!IsReady(p)) return false;
        return true;
    }

    public bool IsLocalReady()
    {
        if (!localPlayer.HasValue) return false;
        return IsReady(localPlayer.Value);
    }

    public void RequestStartRace()
    {
        StartRaceServer();
    }

    [ServerRpc(requireOwnership: false)]
    private void StartRaceServer()
    {
        if (IsRaceActive || CountdownActive) return;
        if (!AreAllPlayersReady()) return;

        if (botSpawner != null)
            botSpawner.SpawnBotsForHumanCount(_playerOrder.Count);

        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        BroadcastCountdownStart();
        for (int t = countdownSeconds; t > 0; t--)
        {
            BroadcastCountdownTick(t);
            yield return new WaitForSeconds(1f);
        }
        BroadcastRaceStarted();
    }

    [ObserversRpc(bufferLast: true)]
    private void BroadcastCountdownStart()
    {
        CountdownActive = true;
        CountdownValue = countdownSeconds;
        OnLobbyStateChanged?.Invoke();
    }

    [ObserversRpc(bufferLast: true)]
    private void BroadcastCountdownTick(int value)
    {
        CountdownActive = true;
        CountdownValue = value;
        OnCountdownTick?.Invoke(value);
        OnLobbyStateChanged?.Invoke();
    }

    [ObserversRpc(bufferLast: true)]
    private void BroadcastRaceStarted()
    {
        CountdownActive = false;
        CountdownValue = 0;
        IsRaceActive = true;
        OnRaceStarted?.Invoke();
        OnLobbyStateChanged?.Invoke();
    }

    private bool[] BuildReadySnapshot()
    {
        var ready = new bool[_playerOrder.Count];
        for (int i = 0; i < _playerOrder.Count; i++)
            ready[i] = IsReady(_playerOrder[i]);
        return ready;
    }

    private string[] BuildDisplayNameSnapshot()
    {
        var names = new string[_playerOrder.Count];
        for (int i = 0; i < _playerOrder.Count; i++)
            names[i] = GetDisplayName(_playerOrder[i]);
        return names;
    }

    private string NormalizeDisplayName(string displayName, PlayerID fallbackId)
    {
        string normalized = displayName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = fallbackId.ToString();

        if (maxDisplayNameLength > 0 && normalized.Length > maxDisplayNameLength)
            normalized = normalized.Substring(0, maxDisplayNameLength);

        return normalized;
    }
}
