using PurrNet;
using UnityEngine;

public class VehicleNetConfig : NetworkIdentity
{
    [SerializeField] private VehicleLoader loader;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        // SOLO l'owner legge il suo file e manda la config al server
        if (isOwner)
        {
            string json = VehicleManager.Instance.GetVehicleJson();
            SubmitConfig_ServerRpc(json);
        }
    }

    [ServerRpc]
    private void SubmitConfig_ServerRpc(string json)
    {
        // il server ridistribuisce a tutti
        ApplyConfig_ObserversRpc(json);
    }

    [ObserversRpc]
    private void ApplyConfig_ObserversRpc(string json)
    {
        loader.ApplyConfigJson(json);
    }
}