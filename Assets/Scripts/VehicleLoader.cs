using System.Collections.Generic;
using UnityEngine;

public class VehicleLoader : MonoBehaviour
{
    [SerializeField] private Composer composer;


    void Awake()
    {
        Dictionary<string, GameObject> list = VehicleManager.Instance.LoadVehicleData();

        if (list.ContainsKey("body") && list["body"] != null && list.ContainsKey("base") && list["base"] != null)
        {
            composer.baseElement = Instantiate(list["base"]);
            composer.bodyElement = Instantiate(list["body"]);

            composer.AlignComponents();
        }

        else
        {
            Debug.Log("Vehicle loader: Errore durante il caricamento del veicolo");
        }
    }
}