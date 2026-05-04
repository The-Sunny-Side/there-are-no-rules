using PurrNet;
using UnityEngine;

public class CheckpointTrigger : NetworkBehaviour
{
    void OnTriggerEnter(Collider collider)
    {
        if (!isServer) return;

        var ai = collider.gameObject.GetComponentInParent<AIRespawner>();
        if (ai != null)
        {
            ai.RegisterCheckpoint(transform.position, transform.rotation);
            return;
        }

        var player = collider.gameObject.GetComponentInParent<PlayerIdentity>();
        if (player == null || !player.TryGetOwner(out PlayerID playerId)) return;

        RespawnerManager.Instance.RegisterCheckpoint(playerId, transform.position, transform.rotation);
    }
}
