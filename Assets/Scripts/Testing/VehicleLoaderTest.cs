using UnityEngine;

public class VehicleLoaderTest : MonoBehaviour
{
    [SerializeField] private VehicleLoader[] vehicleLoaders;
    [SerializeField] private VehicleManager vehicleManager;

    void Start()
    {
        string res = vehicleManager.GetVehicleJson();
        foreach (VehicleLoader loader in vehicleLoaders)
        {
            loader.ApplyConfigJson(res);
        }
    }

}
