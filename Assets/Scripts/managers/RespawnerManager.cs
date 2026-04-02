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

    private void OnDestroy()
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
            var movement = obj.GetComponentInParent<MovementPredicted>();
            if (movement == null || !movement.isOwner) continue;

            var rb = movement.GetComponent<Rigidbody>();
            if (rb == null) continue;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            rb.rotation = rotation;
            return;
        }
    }
}
