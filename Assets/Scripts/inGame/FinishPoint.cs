using PurrNet;
using System;
using System.Collections.Generic;
using UnityEngine;

public class FinishPoint : NetworkBehaviour
{
    [SerializeField] private NetworkManager netManager;
    [SerializeField] private MenuManager menuManager;

    private readonly List<PlayerID> _playerList = new();
    private readonly List<FinishEntry> _finishOrder = new();

    private struct FinishEntry : IEquatable<FinishEntry>
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

    void Start()
    {
        netManager.onPlayerJoined += OnPlayerJoined;
        netManager.onPlayerLeft += OnPlayerLeft;
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

        _finishOrder.Add(finishedEntry);
        BroadcastFinishState();
    }

    [ObserversRpc]
    private void RpcOnPlayerFinished(PlayerID[] finishHumanIds, bool[] finishIsBot, int[] finishBotIds, int totalPlayers)
    {
        menuManager.OnPlayerFinished(finishHumanIds, finishIsBot, finishBotIds, localPlayer, totalPlayers);
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

    private void BroadcastFinishState()
    {
        int count = _finishOrder.Count;
        var finishHumanIds = new PlayerID[count];
        var finishIsBot = new bool[count];
        var finishBotIds = new int[count];

        for (int i = 0; i < count; i++)
        {
            FinishEntry entry = _finishOrder[i];
            finishHumanIds[i] = entry.playerId;
            finishIsBot[i] = entry.isBot;
            finishBotIds[i] = entry.botRaceId;
        }

        RpcOnPlayerFinished(finishHumanIds, finishIsBot, finishBotIds, GetTotalRacers());
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
