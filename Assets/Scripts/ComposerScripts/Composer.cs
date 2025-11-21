#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Composer : MonoBehaviour
{
    public GameObject baseElement;
    public GameObject bodyElement;
    [SerializeField] private float composerSpeed = 5f;
    [SerializeField] private GameObject vehicle;

    private Transform anchor1;
    private Transform anchor2;
    private bool aligning = false;


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
