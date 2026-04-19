using UnityEngine;

public class KillZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerSphere")) return;

        var player = other.GetComponentInParent<PlayerIdentity>();
        if (player == null || !player.IsLocalOwner) return;

        RespawnerManager.Instance?.RequestRespawnLocal();
    }
}
