using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class Vehicle
{
    public string baseElement;
    public string bodyElement;
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

    public void SaveVehicleData(GameObject baseElement, GameObject bodyElement)
    {
        Vehicle data = new Vehicle();

        data.baseElement = baseElement.name;
        data.bodyElement = bodyElement.name;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(configPath, json);

        Debug.Log("Veicolo salvato: Base=" + data.baseElement + ", Body=" + data.bodyElement);
    }

    public Dictionary<string, GameObject> LoadVehicleData()
    {
        Dictionary<string, GameObject> result = new Dictionary<string, GameObject>();
        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            Vehicle data = JsonUtility.FromJson<Vehicle>(json);

            Debug.Log("Veicolo caricato: Base=" + data.baseElement + ", Body=" + data.bodyElement);

            GameObject bodyObj = Resources.Load<GameObject>("bodyElements/" + data.bodyElement);
            GameObject baseObj = Resources.Load<GameObject>("baseElements/" + data.baseElement);


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