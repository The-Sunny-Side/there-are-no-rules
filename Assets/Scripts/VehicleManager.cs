using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class Vehicle
{
    public string baseElement;
    public string bodyElement;
    public string armorLeftElement;
    public string armorRightElement;
    public string armorFrontElement;
    public string armorBackElement;
}

public class VehicleManager : MonoBehaviour
{
    string configPath = "";
    private string defaultBody = "body1";
    private string defaultBase = "base1";

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

    public void SaveVehicleData(GameObject baseElement, GameObject bodyElement, GameObject[] armors)
    {
        Vehicle dataToSave = new Vehicle();

        dataToSave.baseElement = baseElement.name;
        dataToSave.bodyElement = bodyElement.name;
        dataToSave.armorFrontElement = armors[0]?.name ?? null;
        dataToSave.armorLeftElement = armors[1]?.name ?? null;
        dataToSave.armorBackElement = armors[2]?.name ?? null;
        dataToSave.armorRightElement = armors[3]?.name ?? null;

        string json = JsonUtility.ToJson(dataToSave, true);
        File.WriteAllText(configPath, json);

        Debug.Log("Veicolo salvato: "+ dataToSave);
    }

    public Dictionary<string, GameObject> LoadVehicleData()
    {
        Dictionary<string, GameObject> result = new Dictionary<string, GameObject>();
        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            Vehicle data = JsonUtility.FromJson<Vehicle>(json);

            Debug.Log("Veicolo caricato: " + data);

            GameObject bodyObj = Resources.Load<GameObject>("bodyElements/" + data.bodyElement);
            GameObject baseObj = Resources.Load<GameObject>("baseElements/" + data.baseElement);

            if (data.armorRightElement != null) {
                GameObject armorRightObj = Resources.Load<GameObject>("armors/" + data.armorRightElement);
                result["armorRight"] = armorRightObj;
            }

            if (data.armorFrontElement != null)
            {
                GameObject armorFrontObj = Resources.Load<GameObject>("armors/" + data.armorFrontElement);
                result["armorFront"] = armorFrontObj;
            }

            if (data.armorBackElement != null)
            {
                GameObject armorRightObj = Resources.Load<GameObject>("armors/" + data.armorBackElement);
                result["armorBack"] = armorRightObj;
            }

            if (data.armorLeftElement != null)
            {
                GameObject armorLeftObj = Resources.Load<GameObject>("armors/" + data.armorLeftElement);
                result["armorLeft"] = armorLeftObj;
            }

            result["body"] = bodyObj??defaultBodyElement;
            result["base"] = baseObj??defaultBaseElement;

        }
        else
        {
            Debug.LogWarning("File di salvataggio non trovato");
        }
        return result;
    }
}