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

    // Verità server. Mirrorata sui client tramite ObserversRpc bufferLast.
    private readonly Dictionary<PlayerID, bool> _readyStates = new();
    private readonly List<PlayerID> _playerOrder = new();

    public IReadOnlyList<PlayerID> Players => _playerOrder;
    public int PlayerCount => _playerOrder.Count;
    public bool IsReady(PlayerID id) => _readyStates.TryGetValue(id, out var r) && r;

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

        // Fallback: se il client locale è già connesso (caso host) prima dell'OnSpawned,
        // l'evento di join è già passato. Lo aggiungiamo manualmente.
        if (isServer && localPlayer.HasValue)
        {
            HandlePlayerJoined(localPlayer.Value, false, true);
        }
    }

    private void HandlePlayerJoined(PlayerID player, bool isReconnect, bool asServer)
    {
        if (!asServer) return;
        if (_playerOrder.Contains(player)) return;
        _playerOrder.Add(player);
        _readyStates[player] = false;
        BroadcastLobbySnapshot(_playerOrder.ToArray(), BuildReadySnapshot());
    }

    private void HandlePlayerLeft(PlayerID player, bool asServer)
    {
        if (!asServer) return;
        if (!_playerOrder.Remove(player)) return;
        _readyStates.Remove(player);
        BroadcastLobbySnapshot(_playerOrder.ToArray(), BuildReadySnapshot());
    }

    [ObserversRpc(bufferLast: true)]
    private void BroadcastLobbySnapshot(PlayerID[] players, bool[] readyStates)
    {
        _playerOrder.Clear();
        _readyStates.Clear();

        int count = players != null ? players.Length : 0;
        for (int i = 0; i < count; i++)
        {
            PlayerID player = players[i];
            _playerOrder.Add(player);

            bool ready = readyStates != null && i < readyStates.Length && readyStates[i];
            _readyStates[player] = ready;
        }

        OnLobbyStateChanged?.Invoke();
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
        BroadcastLobbySnapshot(_playerOrder.ToArray(), BuildReadySnapshot());
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
}
