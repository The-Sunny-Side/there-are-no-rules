using UnityEngine;

public class KillZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerSphere")) return;

        var movement = other.GetComponentInParent<MovementPredicted>();
        if (movement == null || !movement.isOwner) return;

        RespawnerManager.Instance?.RequestRespawnLocal();
    }
}
