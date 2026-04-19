using PurrNet;
using PurrNet.Prediction;
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
            RequestRespawnLocal();
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
        RespawnRegisteredPlayer(playerId);
    }

    // Chiamato dal client locale (es. da KillZoneTrigger o tasto R)
    public void RequestRespawnLocal()
    {
        if (!localPlayer.HasValue) return;

        if (isServer)
        {
            RespawnRegisteredPlayer(localPlayer.Value);
            return;
        }

        RequestRespawn();
    }

    [ServerRpc(requireOwnership: false)]
    private void RequestRespawn(RPCInfo info = default)
    {
        RespawnRegisteredPlayer(info.sender);
    }

    [TargetRpc]
    private void TeleportPlayerClient(PlayerID target, Vector3 position, Quaternion rotation)
    {
        foreach (var player in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))
        {
            if (player == null || !player.IsLocalOwner) continue;
            TeleportPlayerIdentity(player, position, rotation);
            return;
        }
    }

    private void RespawnPlayer(PlayerID target, Vector3 checkpointPosition, Quaternion rotation)
    {
        Vector3 position = checkpointPosition + Vector3.up * 10f;
        TeleportPlayerServer(target, position, rotation);
        TeleportPlayerClient(target, position, rotation);
    }

    private void RespawnRegisteredPlayer(PlayerID playerId)
    {
        if (!_lastCheckpoints.TryGetValue(playerId, out Vector3 pos)) return;
        _lastRotations.TryGetValue(playerId, out Quaternion rot);
        RespawnPlayer(playerId, pos, rot);
    }

    private static void TeleportPlayerServer(PlayerID target, Vector3 position, Quaternion rotation)
    {
        foreach (var player in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))
        {
            if (player == null || !player.TryGetOwner(out PlayerID owner) || owner != target) continue;
            TeleportPlayerIdentity(player, position, rotation);
            return;
        }
    }

    private static void TeleportPlayerIdentity(PlayerIdentity player, Vector3 position, Quaternion rotation)
    {
        var rb = player.Rigidbody;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            rb.rotation = rotation;
            rb.transform.SetPositionAndRotation(position, rotation);
            rb.WakeUp();
        }
        else
        {
            player.transform.SetPositionAndRotation(position, rotation);
        }

        var predictedTransform = player.PredictedTransform;
        if (predictedTransform != null)
        {
            ref PredictedTransformState transformState = ref predictedTransform.currentState;
            transformState.SetPositionAndRotation(position, rotation);
            predictedTransform.ResetInterpolation();
        }

        var predictedRigidbody = player.PredictedRigidbody;
        if (predictedRigidbody != null)
        {
            predictedRigidbody.linearVelocity = Vector3.zero;
            predictedRigidbody.angularVelocity = Vector3.zero;
            ref UnityRigidbodyState rigidbodyState = ref predictedRigidbody.currentState;
            rigidbodyState.linearVelocity = Vector3.zero;
            rigidbodyState.angularVelocity = Vector3.zero;
            predictedRigidbody.ResetInterpolation();
        }
    }
}
