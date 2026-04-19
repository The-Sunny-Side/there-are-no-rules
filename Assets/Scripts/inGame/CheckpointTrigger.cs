using PurrNet;
using UnityEngine;

public class CheckpointTrigger : NetworkBehaviour
{
    void OnTriggerEnter(Collider collider)
    {
        if (!isServer) return;
        if (!collider.gameObject.CompareTag("PlayerSphere")) return;

        var player = collider.gameObject.GetComponentInParent<PlayerIdentity>();
        if (player == null || !player.TryGetOwner(out PlayerID playerId)) return;

        RespawnerManager.Instance.RegisterCheckpoint(playerId, transform.position, transform.rotation);
    }
}
