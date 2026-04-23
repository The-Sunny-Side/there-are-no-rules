using PurrNet;
using UnityEngine;

[RequireComponent(typeof(PlayerIdentity))]
public class AIRespawner : NetworkBehaviour
{
    [Tooltip("Se true, quando il bot entra in una killzone senza aver mai toccato un checkpoint, viene respawnato all'ultima posizione di spawn.")]
    [SerializeField] private bool fallbackToSpawnPosition = true;

    private PlayerIdentity _identity;
    private Vector3 _checkpointPos;
    private Quaternion _checkpointRot;
    private bool _hasCheckpoint;

    void Awake()
    {
        _identity = GetComponent<PlayerIdentity>();
        _checkpointPos = transform.position;
        _checkpointRot = transform.rotation;
        _hasCheckpoint = fallbackToSpawnPosition;
    }

    public void RegisterCheckpoint(Vector3 position, Quaternion rotation)
    {
        if (!isServer) return;
        _checkpointPos = position;
        _checkpointRot = rotation;
        _hasCheckpoint = true;
    }

    public void RequestRespawn()
    {
        if (!isServer) return;
        if (!_hasCheckpoint) return;

        Vector3 position = _checkpointPos + Vector3.up * 10f;
        RespawnerManager.TeleportPlayerIdentity(_identity, position, _checkpointRot);

        var driver = GetComponent<AIDriver>();
        if (driver != null) driver.ResetTracking();
    }
}
