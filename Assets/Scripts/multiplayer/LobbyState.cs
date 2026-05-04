using System;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Modules;
using UnityEngine;

// Singleton di scena. Server-auth: gestisce ready states, click Play dell'host, countdown e attivazione gara.
// Stato replicato ai client tramite SyncList/SyncDictionary/SyncTimer (auto-buffering per i late joiner).
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

    // Verità server. Replication + buffering per late joiner gestiti da PurrNet.
    private readonly SyncList<PlayerID> _playerOrder = new();
    private readonly SyncDictionary<PlayerID, bool> _readyStates = new();
    private readonly SyncDictionary<PlayerID, string> _displayNames = new();

    // Countdown server-auth con riconciliazione e buffering per late joiner gestiti da PurrNet.
    private readonly SyncTimer _countdownTimer = new();

    public IReadOnlyList<PlayerID> Players => _playerOrder.list;
    public int PlayerCount => _playerOrder.Count;
    public bool IsReady(PlayerID id) => _readyStates.TryGetValue(id, out var r) && r;
    public string GetDisplayName(PlayerID id) =>
        _displayNames.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : id.ToString();

    public bool IsRaceActive { get; private set; }
    public bool CountdownActive => _countdownTimer.isRunning;
    public int CountdownValue => _countdownTimer.remainingInt;

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
        _countdownTimer.onTimerStart -= HandleCountdownTick;
        _countdownTimer.onTimerSecondTick -= HandleCountdownTick;
        _countdownTimer.onTimerEnd -= HandleCountdownEnd;
        _playerOrder.onChanged -= HandlePlayerOrderChanged;
        _readyStates.onChanged -= HandleReadyDictChanged;
        _displayNames.onChanged -= HandleNameDictChanged;
        if (Instance == this) Instance = null;
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        if (netManager != null)
        {
            netManager.onPlayerJoined += HandlePlayerJoined;
            netManager.onPlayerLeft += HandlePlayerLeft;
        }

        _countdownTimer.onTimerStart += HandleCountdownTick;
        _countdownTimer.onTimerSecondTick += HandleCountdownTick;
        _countdownTimer.onTimerEnd += HandleCountdownEnd;

        _playerOrder.onChanged += HandlePlayerOrderChanged;
        _readyStates.onChanged += HandleReadyDictChanged;
        _displayNames.onChanged += HandleNameDictChanged;

        // Forza un primo refresh così la UI vede subito uno stato eventualmente già popolato dal buffering.
        OnLobbyStateChanged?.Invoke();
    }

    private void HandlePlayerOrderChanged(SyncListChange<PlayerID> change) => OnLobbyStateChanged?.Invoke();
    private void HandleReadyDictChanged(SyncDictionaryChange<PlayerID, bool> change) => OnLobbyStateChanged?.Invoke();
    private void HandleNameDictChanged(SyncDictionaryChange<PlayerID, string> change) => OnLobbyStateChanged?.Invoke();

    private void HandleCountdownTick()
    {
        OnCountdownTick?.Invoke(_countdownTimer.remainingInt);
        OnLobbyStateChanged?.Invoke();
    }

    private void HandleCountdownEnd()
    {
        OnLobbyStateChanged?.Invoke();
        if (isServer) BroadcastRaceStarted();
    }

    private void HandlePlayerJoined(PlayerID player, bool isReconnect, bool asServer)
    {
        if (!asServer) return;
        if (_playerOrder.Contains(player)) return;
        _playerOrder.Add(player);
        _readyStates[player] = false;
        _displayNames[player] = NormalizeDisplayName(ResolveAuthName(player), player);
    }

    private void HandlePlayerLeft(PlayerID player, bool asServer)
    {
        if (!asServer) return;
        if (!_playerOrder.Remove(player)) return;
        _readyStates.Remove(player);
        _displayNames.Remove(player);
    }

    // Pesca il nome dal payload di autenticazione PurrNet (vedi NameAuthenticator).
    // Disponibile su tutti i client appena il player è autenticato, prima di onPlayerJoined.
    private string ResolveAuthName(PlayerID player)
    {
        if (networkManager != null &&
            networkManager.TryGetModule<PlayersManager>(true, out var players) &&
            players.TryGetConnection(player, out var conn) &&
            NameAuthenticator.TryGetName(conn, out var name))
        {
            return name;
        }
        return null;
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
        _readyStates[info.sender] = !_readyStates[info.sender];
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

        _countdownTimer.StartTimer(countdownSeconds);
    }

    [ObserversRpc(bufferLast: true)]
    private void BroadcastRaceStarted()
    {
        IsRaceActive = true;
        OnRaceStarted?.Invoke();
        OnLobbyStateChanged?.Invoke();
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
