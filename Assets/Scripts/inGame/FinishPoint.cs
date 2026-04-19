using PurrNet;
using System.Collections.Generic;
using UnityEngine;

public class FinishPoint : NetworkBehaviour
{
    [SerializeField] private NetworkManager netManager;
    [SerializeField] private MenuManager menuManager;

    private readonly List<PlayerID> _playerList = new();
    private readonly List<PlayerID> _finishOrder = new();

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
        if (player == null || !player.TryGetOwner(out PlayerID finishedId)) return;

        if (_finishOrder.Contains(finishedId)) return;

        _finishOrder.Add(finishedId);
        RpcOnPlayerFinished(_finishOrder.ToArray(), _playerList.Count);
    }

    [ObserversRpc]
    private void RpcOnPlayerFinished(PlayerID[] finishOrder, int totalPlayers)
    {
        menuManager.OnPlayerFinished(finishOrder, localPlayer, totalPlayers);
    }
}
