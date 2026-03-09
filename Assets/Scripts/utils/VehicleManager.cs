using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class Vehicle
{
    public string baseElement;
    public string bodyElement;
    public string weaponLeft;
    public string weaponRight;
    public string weaponFront;
    public string weaponBack;

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

    void Awake()
    {
        if (Instance == null)
        {
            configPath = Application.persistentDataPath + "/vehicle_config.json";
            Instance = this;
            defaultBaseElement = vehiclePrefabRegistry.GetBase(defaultBase).element;
            defaultBodyElement = vehiclePrefabRegistry.GetBody(defaultBody).element;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveVehicleData(GameObject baseElement, GameObject bodyElement, GameObject[] weapons)
    {
        Vehicle dataToSave = new Vehicle();

        dataToSave.baseElement = baseElement.name;
        dataToSave.bodyElement = bodyElement.name;
        dataToSave.weaponFront = weapons[0]?.name ?? null;
        dataToSave.weaponLeft = weapons[1]?.name ?? null;
        dataToSave.weaponBack = weapons[2]?.name ?? null;
        dataToSave.weaponRight = weapons[3]?.name ?? null;

        string json = JsonUtility.ToJson(dataToSave, true);
        File.WriteAllText(configPath, json);

        Debug.Log("Veicolo salvato: " + json);
    }

    public Dictionary<string, GameObject> LoadVehicleData()
    {
        Dictionary<string, GameObject> result = new Dictionary<string, GameObject>();
        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            Vehicle data = JsonUtility.FromJson<Vehicle>(json);

            GameObject bodyObj = vehiclePrefabRegistry.GetBody(data.bodyElement).element;
            GameObject baseObj = vehiclePrefabRegistry.GetBase(data.baseElement).element;

            if (data.weaponRight != null)
            {
                GameObject weaponRightObj = vehiclePrefabRegistry.GetWeapon(data.weaponRight).element;
                result[VehicleElementsKeys.WeaponRight] = weaponRightObj;
            }

            if (data.weaponFront != null)
            {
                GameObject weaponFrontObj = vehiclePrefabRegistry.GetWeapon(data.weaponFront).element;
                result[VehicleElementsKeys.WeaponFront] = weaponFrontObj;
            }

            if (data.weaponBack != null)
            {
                GameObject weaponBackObj = vehiclePrefabRegistry.GetWeapon(data.weaponBack).element;
                result[VehicleElementsKeys.WeaponBack] = weaponBackObj;
            }

            if (data.weaponLeft != null)
            {
                GameObject weaponLeftObj = vehiclePrefabRegistry.GetWeapon(data.weaponLeft).element;
                result[VehicleElementsKeys.WeaponLeft] = weaponLeftObj;
            }

            result[VehicleElementsKeys.Body] = bodyObj ?? defaultBodyElement;
            result[VehicleElementsKeys.Base] = baseObj ?? defaultBaseElement;

            Debug.Log("Veicolo caricato:");
            foreach(var item in result)
            {
                Debug.Log(item.Key, item.Value);
            }


        }
        else
        {
            Debug.LogWarning("File di salvataggio non trovato");
        }
        return result;
    }
    public string GetVehicleJson()
    {
        if (File.Exists(configPath))
            return File.ReadAllText(configPath);
        return "";

    }
}