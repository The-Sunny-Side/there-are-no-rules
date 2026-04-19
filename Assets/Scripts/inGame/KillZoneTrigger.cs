using UnityEngine;

public class KillZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerIdentity>();
        if (player == null || !player.IsLocalOwner) return;

        RespawnerManager.Instance?.RequestRespawnLocal();
    }
}
