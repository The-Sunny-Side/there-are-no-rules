#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Composer : MonoBehaviour
{
    public GameObject baseElement;
    public GameObject bodyElement;
    [SerializeField] private float composerSpeed = 5f;

    private Transform anchor1;
    private Transform anchor2;
    private bool aligning = false;

    private GameObject vehicleRoot;

   
  

    void Update()
    {
        if (!aligning) return;

        Vector3 offset = anchor1.position - anchor2.position;
        Vector3 targetPos = bodyElement.transform.position + offset;

        bodyElement.transform.position = Vector3.Lerp(
            bodyElement.transform.position,
            targetPos,
            Time.deltaTime * composerSpeed);


        if (Vector3.Distance(anchor1.position, anchor2.position) < 0.001f)
        {
            aligning = false;
            CreateVehicleGameObject();

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
        //aligning = true;
        ComposeVehicle();

    }

    void CreateVehicleGameObject()
    {
        // 1) creo il root del veicolo
        vehicleRoot = new GameObject("vehicle");

        // lo metto dove sta la base (così ha un senso)
        vehicleRoot.transform.position = baseElement.transform.position;
        vehicleRoot.transform.rotation = baseElement.transform.rotation;

        // se vuoi tenerlo sotto Composer, fallo ma mantenendo la world position
        vehicleRoot.transform.SetParent(transform, false); // true = mantieni world

        // 2) metto base e body dentro vehicle MA mantenendo la loro posizione nel mondo
        baseElement.transform.SetParent(vehicleRoot.transform, false); // true = non si spostano
        bodyElement.transform.SetParent(vehicleRoot.transform, false); // idem
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

        PrefabUtility.SaveAsPrefabAsset(vehicleRoot, prefabPath);
        AssetDatabase.SaveAssets();
        Debug.Log("Prefab salvato in: " + prefabPath);
    }
#else
    public void SaveVehiclePrefab() { }
#endif
}
