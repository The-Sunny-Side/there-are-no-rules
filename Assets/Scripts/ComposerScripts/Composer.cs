#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Composer : MonoBehaviour
{
    public GameObject baseElement;
    public GameObject bodyElement;
    public GameObject armorLeftElement;
    public GameObject armorRightElement;
    public GameObject armorFrontElement;
    public GameObject armorBackElement;
    [SerializeField] private float composerSpeed = 5f;
    [SerializeField] private GameObject vehicle;

    private Transform anchor1;
    private Transform anchor2;


    private void OnDisable()
    {
        foreach (Transform child in vehicle.transform)
        {
            Destroy(child.gameObject);
        }
    }

    void ComposeVehicle()
    {
        if (anchor1 == null || anchor2 == null)
        {
            Debug.LogError("Anchor non trovate");
            return;
        }

        Vector3 delta = anchor1.position - anchor2.position;

        bodyElement.transform.position += delta;
        CreateVehicleGameObject();
    }

    public void AlignComponents()
    {
        anchor1 = baseElement.transform.Find("anchor");
        anchor2 = bodyElement.transform.Find("anchor");
        ComposeVehicle();

    }

    void CreateVehicleGameObject()
    {
        Transform armorRightContainer = bodyElement.transform.Find("armorRightAnchor");
        Transform armorLeftContainer = bodyElement.transform.Find("armorLeftAnchor");
        Transform armorFrontContainer = bodyElement.transform.Find("armorFrontAnchor");
        Transform armorBackContainer = bodyElement.transform.Find("armorBackAnchor");


        armorLeftElement.transform.SetParent(armorLeftContainer.transform, false);
        armorLeftElement.transform.rotation = armorLeftContainer.rotation;
        armorLeftElement.transform.localScale = Vector3.Scale(armorLeftContainer.localScale, new Vector3(1f, 1f, 1f));

        armorRightElement.transform.SetParent(armorRightContainer.transform, false);
        armorRightElement.transform.rotation = armorRightContainer.rotation;
        armorRightElement.transform.localScale = Vector3.Scale(armorRightContainer.localScale,new Vector3(1f,1f,1f));

        armorFrontElement.transform.SetParent(armorFrontContainer.transform, false);
        armorFrontElement.transform.rotation = armorFrontContainer.rotation;
        armorFrontElement.transform.localScale = Vector3.Scale(armorFrontContainer.localScale, new Vector3(1f, 1f, 1f));

        armorBackElement.transform.SetParent(armorBackContainer.transform, false);
        armorBackElement.transform.rotation = armorBackContainer.rotation;
        armorBackElement.transform.localScale = Vector3.Scale(armorBackContainer.localScale, new Vector3(1f, 1f, 1f));

        baseElement.transform.SetParent(vehicle.transform, false);
        bodyElement.transform.SetParent(vehicle.transform, false);
    }

#if UNITY_EDITOR
    public void SaveVehiclePrefab()
    {
        const string folderPath = "Assets/Prefabs/composedVehicles";

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "composedVehicles");
        }

        string prefabPath = AssetDatabase.GenerateUniqueAssetPath(
            folderPath + "/vehicle.prefab");

        PrefabUtility.SaveAsPrefabAsset(vehicle, prefabPath);
        AssetDatabase.SaveAssets();
        Debug.Log("Prefab salvato in: " + prefabPath);
    }
#else
//todo: implement runtime saving
    public void SaveVehiclePrefab() { }
#endif
}
