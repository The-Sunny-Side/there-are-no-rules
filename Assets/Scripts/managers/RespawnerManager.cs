using PurrNet;
using System.Collections.Generic;
using UnityEngine;

public class RespawnerManager : NetworkBehaviour
{
    public static RespawnerManager Instance { get; private set; }

    // Server-side: ultimo checkpoint toccato per ogni player
    private readonly Dictionary<PlayerID, Vector3> _lastCheckpoints = new();
    private readonly Dictionary<PlayerID, Quaternion> _lastRotations = new();

    protected override void OnSpawned()
    {
        base.OnSpawned();
        Instance = this;
    }

    private new void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!isClient) return;
        if (!localPlayer.HasValue) return;
        if (Input.GetKeyDown(KeyCode.R))
            RequestRespawn(localPlayer.Value);
    }

    // Chiamato da CheckpointTrigger (solo server)
    public void RegisterCheckpoint(PlayerID playerId, Vector3 position, Quaternion rotation)
    {
        if (!isServer) return;
        _lastCheckpoints[playerId] = position;
        _lastRotations[playerId] = rotation;
    }

    // Chiamato direttamente server-side (es. da KillZoneTrigger)
    public void ForceRespawn(PlayerID playerId)
    {
        if (!isServer) return;
        if (!_lastCheckpoints.TryGetValue(playerId, out Vector3 pos)) return;
        _lastRotations.TryGetValue(playerId, out Quaternion rot);
        TeleportPlayer(playerId, pos, rot);
    }

    // Chiamato dal client locale (es. da KillZoneTrigger o tasto R)
    public void RequestRespawnLocal()
    {
        if (!localPlayer.HasValue) return;
        RequestRespawn(localPlayer.Value);
    }

    [ServerRpc(requireOwnership: false)]
    private void RequestRespawn(PlayerID requestingPlayer)
    {
        if (!_lastCheckpoints.TryGetValue(requestingPlayer, out Vector3 pos)) return;
        _lastRotations.TryGetValue(requestingPlayer, out Quaternion rot);
        TeleportPlayer(requestingPlayer, pos, rot);
    }

    [TargetRpc]
    private void TeleportPlayer(PlayerID target, Vector3 position, Quaternion rotation)
    {
        foreach (var obj in GameObject.FindGameObjectsWithTag("PlayerSphere"))
        {
            var player = obj.GetComponentInParent<PlayerIdentity>();
            if (player == null || !player.IsLocalOwner) continue;

            var rb = player.Rigidbody;
            if (rb == null) continue;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position + Vector3.up * 10f;
            rb.rotation = rotation;
            return;
        }
    }
}
