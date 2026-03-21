using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static VehiclePrefabRegistry;

[System.Serializable]
public class Vehicle
{
    public string baseElement;
    public string bodyElement;
    public string weaponLeft;
    public string weaponRight;
    public string weaponFront;
    public string weaponBack;

    private Dictionary<string, VehicleEntry> savedVehicle = new Dictionary<string, VehicleEntry>();

    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(baseElement) || baseElement == "empty") return false;
        if (string.IsNullOrWhiteSpace(bodyElement) || bodyElement == "empty") return false;
        return true;
    }
}

public class VehicleManager : MonoBehaviour
{
    string configPath = "";
    private string defaultBody = "cube";
    private string defaultBase = "plane";

    private GameObject defaultBaseElement;
    private GameObject defaultBodyElement;

    public static VehicleManager Instance { get; private set; }

    [SerializeField]
    public VehiclePrefabRegistry vehiclePrefabRegistry;

    private bool resyncVehicle = true;
    private Dictionary<string, VehicleEntry> savedVehicle = new Dictionary<string, VehicleEntry>();

    void Awake()
    {
        if (Instance == null)
        {
            configPath = Application.persistentDataPath + "/vehicle_config.json";
            Instance = this;
            defaultBaseElement = vehiclePrefabRegistry.GetBase(defaultBase).element;
            defaultBodyElement = vehiclePrefabRegistry.GetBody(defaultBody).element;

            if(!File.Exists(configPath))
            {
                Vehicle defaultVehicle = new Vehicle
                {
                    baseElement = defaultBase,
                    bodyElement = defaultBody,
                    weaponFront = "empty",
                    weaponLeft = "empty",
                    weaponBack = "empty",
                    weaponRight = "empty"
                };
                string json = JsonUtility.ToJson(defaultVehicle, true);
                File.WriteAllText(configPath, json);
            }else {                 string json = File.ReadAllText(configPath);
                Vehicle data = JsonUtility.FromJson<Vehicle>(json);
                savedVehicle = GetVehicleElements(data);
            }
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveVehicleData(VehicleEntry baseElement, VehicleEntry bodyElement, VehicleEntry[] weapons)
    {
        Vehicle dataToSave = new Vehicle();

        dataToSave.baseElement = baseElement.key;
        dataToSave.bodyElement = bodyElement.key;
        dataToSave.weaponFront = weapons[0]?.key ?? null;
        dataToSave.weaponLeft = weapons[1]?.key ?? null;
        dataToSave.weaponBack = weapons[2]?.key ?? null;
        dataToSave.weaponRight = weapons[3]?.key ?? null;

        string json = JsonUtility.ToJson(dataToSave, true);
        File.WriteAllText(configPath, json);

        savedVehicle= GetVehicleElements(dataToSave);
        Debug.Log("Veicolo salvato: " + json);
    }

    private Dictionary<string, VehicleEntry> GetVehicleElements(Vehicle data)
    {
        Dictionary<string, VehicleEntry> result = new Dictionary<string, VehicleEntry>();

        VehicleEntry bodyObj = vehiclePrefabRegistry.GetBody(data.bodyElement);
        VehicleEntry baseObj = vehiclePrefabRegistry.GetBase(data.baseElement);

        if (data.weaponRight != null)
        {
            VehicleEntry weaponRightObj = vehiclePrefabRegistry.GetWeapon(data.weaponRight);
            result[VehicleElementsKeys.WeaponRight] = weaponRightObj;
        }

        if (data.weaponFront != null)
        {
            VehicleEntry weaponFrontObj = vehiclePrefabRegistry.GetWeapon(data.weaponFront);
            result[VehicleElementsKeys.WeaponFront] = weaponFrontObj;
        }

        if (data.weaponBack != null)
        {
            VehicleEntry weaponBackObj = vehiclePrefabRegistry.GetWeapon(data.weaponBack);
            result[VehicleElementsKeys.WeaponBack] = weaponBackObj;
        }

        if (data.weaponLeft != null)
        {
            VehicleEntry weaponLeftObj = vehiclePrefabRegistry.GetWeapon(data.weaponLeft);
            result[VehicleElementsKeys.WeaponLeft] = weaponLeftObj;
        }

        result[VehicleElementsKeys.Body] = bodyObj ?? vehiclePrefabRegistry.GetBody(defaultBody);
        result[VehicleElementsKeys.Base] = baseObj ?? vehiclePrefabRegistry.GetBase(defaultBase);

        return result;
    }

    public Dictionary<string, VehicleEntry> LoadVehicleConfig()
    {
        if (resyncVehicle)
        {
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                Vehicle data = JsonUtility.FromJson<Vehicle>(json);
                savedVehicle = GetVehicleElements(data);
                resyncVehicle = false;
            }
            else
            {
                Debug.LogWarning("File di salvataggio non trovato");
            }
        }
        return savedVehicle;
    }

    public string GetVehicleJson()
    {
        if (File.Exists(configPath))
            return File.ReadAllText(configPath);
        return "";

    }
}