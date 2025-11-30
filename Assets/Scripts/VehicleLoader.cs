using System.Collections.Generic;
using UnityEngine;

public class VehicleLoader : MonoBehaviour
{
    public GameObject baseContainer;
    public GameObject bodyContainer;

    void Awake()
    {
        Debug.Log("VehicleLoader: Inizio caricamento");

        Dictionary<string, GameObject> list = VehicleManager.Instance.LoadVehicleData();

        if (list.ContainsKey("base") && list["base"] != null)
        {
            Debug.Log("Caricamento base: " + list["base"].name);

            GameObject baseInstance = Instantiate(list["base"]);
            baseInstance.transform.SetParent(baseContainer.transform);

            baseInstance.transform.localPosition = Vector3.zero;
            baseInstance.transform.localRotation = Quaternion.identity;
            baseInstance.transform.localScale = Vector3.one;
        }

        if (list.ContainsKey("body") && list["body"] != null)
        {
            Debug.Log("Caricamento body: " + list["body"].name);

            GameObject bodyInstance = Instantiate(list["body"]);
            bodyInstance.transform.SetParent(bodyContainer.transform);

            bodyInstance.transform.localPosition = Vector3.zero;
            bodyInstance.transform.localRotation = Quaternion.identity;
            bodyInstance.transform.localScale = Vector3.one;
        }
    }
}