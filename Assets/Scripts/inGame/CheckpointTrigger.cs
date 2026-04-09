using PurrNet;
using UnityEngine;

public class CheckpointTrigger : NetworkBehaviour
{
    void OnTriggerEnter(Collider collider)
    {
        if (!isServer) return;
        if (!collider.gameObject.CompareTag("PlayerSphere")) return;

        var movement = collider.gameObject.GetComponentInParent<MovementPredicted>();
        if (movement == null || !movement.owner.HasValue) return;

        PlayerID playerId = movement.owner.Value;
        RespawnerManager.Instance.RegisterCheckpoint(playerId, transform.position,transform.rotation);
    }
}
