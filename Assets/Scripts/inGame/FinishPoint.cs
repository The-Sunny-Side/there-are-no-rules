using PurrNet;
using System.Collections.Generic;
using UnityEngine;

public class FinishPoint : NetworkBehaviour
{
    [SerializeField] private NetworkManager netManager;
    [SerializeField] private MenuManager menuManager;

    private readonly List<PlayerID> playerList = new();

    private bool _raceEnded;

    void Start()
    {
        netManager.onPlayerJoined += UpdateList;
        netManager.onPlayerLeft += RemovePlayer;
    }

    private void RemovePlayer(PlayerID player, bool asserver)
    {
        if (!asserver) return;
        playerList.Remove(player);
    }

    private void UpdateList(PlayerID player, bool isreconnect, bool asserver)
    {
        if (!asserver) return;
        if (!playerList.Contains(player))
            playerList.Add(player);
    }

    void OnTriggerEnter(Collider collider)
    {
        if (!isServer) return;
        if (_raceEnded) return;

        if (!collider.gameObject.CompareTag("PlayerSphere"))
            return;

        var movement = collider.gameObject.GetComponentInParent<MovementPredicted>();
        if (!movement) return;

        if (!movement.owner.HasValue) return;
        PlayerID winningId = movement.owner.Value;

        _raceEnded = true;

        Congrats(winningId);

        foreach (var p in playerList)
            if (!p.Equals(winningId))
                LoseMessage(p);
    }

    [TargetRpc]
    private void Congrats(PlayerID target)
    {
        Debug.Log("hai vinto");
        menuManager.OnWinMatch();
    }

    [TargetRpc]
    private void LoseMessage(PlayerID target)
    {
        Debug.Log("hai perso");
        menuManager.OnLoseMatch();
    }
}