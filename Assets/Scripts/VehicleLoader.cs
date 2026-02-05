using System.Collections.Generic;
using UnityEngine;

public class VehicleLoader : MonoBehaviour
{
    [SerializeField] private Composer composer;


    void Awake()
    {
        Dictionary<string, GameObject> list = VehicleManager.Instance.LoadVehicleData();

        foreach(string key in list.Keys)
        {
            Debug.Log(key + ": " + list[key]);
        }

        if (list.ContainsKey("body") && list["body"] != null && list.ContainsKey("base") && list["base"] != null)
        {
            composer.baseElement = Instantiate(list["base"]);
            composer.bodyElement = Instantiate(list["body"]);
            composer.armorBackElement = Instantiate(list["armorBack"]);
            composer.armorFrontElement = Instantiate(list["armorFront"]);
            composer.armorLeftElement = Instantiate(list["armorLeft"]);
            composer.armorRightElement = Instantiate(list["armorRight"]);

            composer.AlignComponents();
        }

        else
        {
            Debug.Log("Vehicle loader: Errore durante il caricamento del veicolo");
        }
    }
}