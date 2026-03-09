using UnityEngine;

public class GameConfig : MonoBehaviour
{
    public static UiConfig UiConfig { get; private set; }

    public static VehiclePrefabRegistry VehiclePrefabRegistry { get; private set; }

    [SerializeField] private UiConfig uiConfig;
    [SerializeField] private VehiclePrefabRegistry vehiclePrefabRegistry;


    private void Awake()
    {
        if (UiConfig == null && VehiclePrefabRegistry == null)
        {
            UiConfig = uiConfig;
            VehiclePrefabRegistry = vehiclePrefabRegistry;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
