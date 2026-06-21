using PurrNet;
using PurrNet.Packing;
using System;
using System.Collections.Generic;
using UnityEngine;

// Stato server-auth: ogni voce è un player o un bot che ha tagliato il traguardo.
// IPackedAuto fa generare a PurrNet il packer per la replica via SyncList.
internal struct FinishEntry : IPackedAuto, IEquatable<FinishEntry>
{
    public bool isBot;
    public PlayerID playerId;
    public int botRaceId;

    public bool Equals(FinishEntry other)
    {
        if (isBot != other.isBot) return false;
        return isBot ? botRaceId == other.botRaceId : playerId == other.playerId;
    }
}

public class FinishPoint : NetworkBehaviour
{
    [SerializeField] private NetworkManager netManager;
    [SerializeField] private MenuManager menuManager;

    private readonly List<PlayerID> _playerList = new();
    // Replicato + buffering per late joiner gestiti da PurrNet.
    private readonly SyncList<FinishEntry> _finishOrder = new();

    protected override void OnSpawned()
    {
        base.OnSpawned();

        // FinishPoint vive dentro il prefab della mappa: i riferimenti a oggetti di
        // scena (NetworkManager, MenuManager) non possono essere serializzati nel prefab,
        // quindi li risolviamo a runtime se non assegnati.
        if (netManager == null) netManager = networkManager;
        if (menuManager == null) menuManager = FindFirstObjectByType<MenuManager>();

        if (netManager == null)
            Debug.LogWarning("[FinishPoint] NetworkManager non trovato: join/leave non verranno tracciati.", this);
        if (menuManager == null)
            Debug.LogWarning("[FinishPoint] MenuManager non trovato in scena: la dashboard di fine gara NON verrà mostrata al traguardo.", this);

        if (netManager != null)
        {
            netManager.onPlayerJoined += OnPlayerJoined;
            netManager.onPlayerLeft += OnPlayerLeft;
        }
        _finishOrder.onChanged += HandleFinishOrderChanged;
    }

    private new void OnDestroy()
    {
        if (netManager != null)
        {
            netManager.onPlayerJoined -= OnPlayerJoined;
            netManager.onPlayerLeft -= OnPlayerLeft;
        }
        _finishOrder.onChanged -= HandleFinishOrderChanged;
    }

    private void OnPlayerJoined(PlayerID player, bool isReconnect, bool asServer)
    {
        if (!asServer) return;
        if (!_playerList.Contains(player))
            _playerList.Add(player);
    }

    private void OnPlayerLeft(PlayerID player, bool asServer)
    {
        if (!asServer) return;
        _playerList.Remove(player);
    }

    void OnTriggerEnter(Collider collider)
    {
        if (!isServer) return;

        var player = collider.gameObject.GetComponentInParent<PlayerIdentity>();
        if (!TryResolveFinishEntry(player, out FinishEntry finishedEntry)) return;
        if (_finishOrder.Contains(finishedEntry)) return;

        _finishOrder.Add(finishedEntry); // SyncList replica e bufferizza per i late joiner
    }

    private void HandleFinishOrderChanged(SyncListChange<FinishEntry> change)
    {
        if (menuManager == null) return;

        int count = _finishOrder.Count;
        var humanIds = new PlayerID[count];
        var isBotArr = new bool[count];
        var botIds = new int[count];
        for (int i = 0; i < count; i++)
        {
            var e = _finishOrder[i];
            humanIds[i] = e.playerId;
            isBotArr[i] = e.isBot;
            botIds[i] = e.botRaceId;
        }

        menuManager.OnPlayerFinished(humanIds, isBotArr, botIds, localPlayer, GetTotalRacers());
    }

    private bool TryResolveFinishEntry(PlayerIdentity player, out FinishEntry entry)
    {
        if (player != null && player.IsBot && player.BotRaceId > 0)
        {
            entry = new FinishEntry
            {
                isBot = true,
                playerId = default,
                botRaceId = player.BotRaceId
            };
            return true;
        }

        if (player != null && player.TryGetOwner(out PlayerID finishedId))
        {
            entry = new FinishEntry
            {
                isBot = false,
                playerId = finishedId,
                botRaceId = 0
            };
            return true;
        }

        entry = default;
        return false;
    }

    private int GetTotalRacers()
    {
        var humanIds = new HashSet<PlayerID>();
        var botIds = new HashSet<int>();

        foreach (var identity in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))
        {
            if (identity == null) continue;

            if (identity.IsBot && identity.BotRaceId > 0)
            {
                botIds.Add(identity.BotRaceId);
                continue;
            }

            if (identity.TryGetOwner(out PlayerID playerId))
                humanIds.Add(playerId);
        }

        int humans = humanIds.Count > 0 ? humanIds.Count : _playerList.Count;
        return humans + botIds.Count;
    }
}
