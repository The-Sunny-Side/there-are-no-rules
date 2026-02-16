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
    [SerializeField] private GameObject vehicle;

    private Transform anchor1;
    private Transform anchor2;

    private void Awake()
    {
        GameObject defaultElement = Resources.Load<GameObject>("/icons/empty");
        armorLeftElement = defaultElement;
        armorBackElement=defaultElement;
        armorFrontElement=defaultElement;
        armorRightElement=defaultElement;
    }

    private void OnDisable()
    {
        foreach (Transform child in vehicle.transform)
        {
            Destroy(child.gameObject);
        }
        Utilities.DestroyAllChildren(vehicle);
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
        anchor1 = baseElement.transform.Find(AnchorNames.StandardAnchor);
        anchor2 = bodyElement.transform.Find(AnchorNames.StandardAnchor);
        ComposeVehicle();

    }

    void CreateVehicleGameObject()
    {
        Transform weaponRightContainer = bodyElement.transform.Find(AnchorNames.WeaponRightAnchor);
        Transform weaponLeftContainer = bodyElement.transform.Find(AnchorNames.WeaponLeftAnchor);
        Transform weaponFrontContainer = bodyElement.transform.Find(AnchorNames.WeaponFrontAnchor);
        Transform weaponBackContainer = bodyElement.transform.Find(AnchorNames.WeaponBackAnchor);

        Utilities.DestroyAllChildren(weaponRightContainer);
        Utilities.DestroyAllChildren(weaponLeftContainer);
        Utilities.DestroyAllChildren(weaponFrontContainer);
        Utilities.DestroyAllChildren(weaponBackContainer);

        armorLeftElement.transform.SetParent(weaponLeftContainer.transform, false);
        armorLeftElement.transform.rotation = weaponLeftContainer.rotation;
        armorLeftElement.transform.localScale = Vector3.Scale(weaponLeftContainer.localScale, new Vector3(1f, 1f, 1f));

        armorRightElement.transform.SetParent(weaponRightContainer.transform, false);
        armorRightElement.transform.rotation = weaponRightContainer.rotation;
        armorRightElement.transform.localScale = Vector3.Scale(weaponRightContainer.localScale,new Vector3(1f,1f,1f));

        armorFrontElement.transform.SetParent(weaponFrontContainer.transform, false);
        armorFrontElement.transform.rotation = weaponFrontContainer.rotation;
        armorFrontElement.transform.localScale = Vector3.Scale(weaponFrontContainer.localScale, new Vector3(1f, 1f, 1f));

        armorBackElement.transform.SetParent(weaponBackContainer.transform, false);
        armorBackElement.transform.rotation = weaponBackContainer.rotation;
        armorBackElement.transform.localScale = Vector3.Scale(weaponBackContainer.localScale, new Vector3(1f, 1f, 1f));

        baseElement.transform.SetParent(vehicle.transform, false);
        baseElement.transform.localPosition = new Vector3(0, baseElement.transform.localPosition.y, baseElement.transform.localPosition.z);

        bodyElement.transform.SetParent(vehicle.transform, false);
        bodyElement.transform.localPosition = new Vector3(0, bodyElement.transform.localPosition.y, bodyElement.transform.localPosition.z);
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

        PrefabUtility.SaveAsPrefabAsset(vehicle, "Assets/Prefabs/composedVehicles/vehicle.prefab");
        AssetDatabase.SaveAssets();
        Debug.Log("Prefab salvato in: " + prefabPath);
    }
#else
//todo: implement runtime saving
    public void SaveVehiclePrefab() { }
#endif
}
