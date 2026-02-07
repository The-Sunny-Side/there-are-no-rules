using PurrNet;
using UnityEngine;

public class VehicleNetConfig : NetworkIdentity
{
    [SerializeField] private VehicleLoader loader;

    // tenuta lato server
    private string _lastJson;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        if (isOwner)
        {
            string json = VehicleManager.Instance.GetVehicleJson();
            SubmitConfig_ServerRpc(json);
        }
    }

    [ServerRpc]
    private void SubmitConfig_ServerRpc(string json)
    {
        _lastJson = json;
        ApplyConfig_ObserversRpc(json);
    }

    // chiamabile SOLO dal server quando entra un nuovo client
    public void ServerResendIfAny()
    {
        if (!isServer) return;
        if (!string.IsNullOrEmpty(_lastJson))
            ApplyConfig_ObserversRpc(_lastJson);
    }

    [ObserversRpc]
    private void ApplyConfig_ObserversRpc(string json)
    {
        loader.ApplyConfigJson(json);
    }
}