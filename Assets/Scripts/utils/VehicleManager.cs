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

    void Awake()
    {
        if (Instance == null)
        {
            configPath = Application.persistentDataPath + "/vehicle_config.json";
            Instance = this;
            defaultBaseElement = Resources.Load<GameObject>("bodyElements/" + defaultBase);
            defaultBodyElement = Resources.Load<GameObject>("bodyElements/" + defaultBody);
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

            GameObject bodyObj = Resources.Load<GameObject>("bodyElements/" + data.bodyElement);
            GameObject baseObj = Resources.Load<GameObject>("baseElements/" + data.baseElement);

            if (data.weaponRight != null)
            {
                GameObject weaponRightObj = Resources.Load<GameObject>("weapons/" + data.weaponRight);
                result[VehicleElementsKeys.WeaponRight] = weaponRightObj;
            }

            if (data.weaponFront != null)
            {
                GameObject weaponFrontObj = Resources.Load<GameObject>("weapons/" + data.weaponFront);
                result[VehicleElementsKeys.WeaponFront] = weaponFrontObj;
            }

            if (data.weaponBack != null)
            {
                GameObject weaponBackObj = Resources.Load<GameObject>("weapons/" + data.weaponBack);
                result[VehicleElementsKeys.WeaponBack] = weaponBackObj;
            }

            if (data.weaponLeft != null)
            {
                GameObject weaponLeftObj = Resources.Load<GameObject>("weapons/" + data.weaponLeft);
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