#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Composer : MonoBehaviour
{
    public GameObject baseElement;
    public GameObject bodyElement;
    public GameObject weaponLeftElement;
    public GameObject weaponRightElement;
    public GameObject weaponFrontElement;
    public GameObject weaponBackElement;

    [SerializeField] private GameObject vehicle;

    private Transform anchor1;
    private Transform anchor2;

    private void Awake()
    {
        GameObject defaultElement = GameConfig.VehiclePrefabRegistry.EmptyPart.element;
        weaponLeftElement = defaultElement;
        weaponBackElement = defaultElement;
        weaponFrontElement = defaultElement;
        weaponRightElement = defaultElement;
    }

    private void OnDisable()
    {
        ClearRuntimeParts();
    }

    void ComposeVehicle()
    {
        if (baseElement == null || bodyElement == null || vehicle == null)
        {
            Debug.LogWarning("Composer: missing base/body/vehicle reference");
            return;
        }

        anchor1 = ResolveAnchor(baseElement.transform, AnchorNames.StandardAnchor);
        anchor2 = ResolveAnchor(bodyElement.transform, AnchorNames.StandardAnchor);

        if (anchor1 != null && anchor2 != null)
        {
            Vector3 delta = anchor1.position - anchor2.position;
            bodyElement.transform.position += delta;
        }
        else
        {
            Debug.LogWarning("Composer: standard anchor missing, composing without alignment");
        }

        CreateVehicleGameObject();
    }

    public void AlignComponents()
    {
        ComposeVehicle();
    }

    void CreateVehicleGameObject()
    {
        if (baseElement == null || bodyElement == null || vehicle == null)
            return;

        // Clear previous composed children before attaching the new set.
        ClearVehicleChildrenKeepingComposer();

        Transform weaponRightContainer = ResolveAnchor(bodyElement.transform, AnchorNames.WeaponRightAnchor);
        Transform weaponLeftContainer = ResolveAnchor(bodyElement.transform, AnchorNames.WeaponLeftAnchor);
        Transform weaponFrontContainer = ResolveAnchor(bodyElement.transform, AnchorNames.WeaponFrontAnchor);
        Transform weaponBackContainer = ResolveAnchor(bodyElement.transform, AnchorNames.WeaponBackAnchor);

        if (weaponRightContainer) Utilities.DestroyAllChildren(weaponRightContainer);
        if (weaponLeftContainer) Utilities.DestroyAllChildren(weaponLeftContainer);
        if (weaponFrontContainer) Utilities.DestroyAllChildren(weaponFrontContainer);
        if (weaponBackContainer) Utilities.DestroyAllChildren(weaponBackContainer);

        AttachArmorToContainer(weaponLeftElement, weaponLeftContainer);
        AttachArmorToContainer(weaponRightElement, weaponRightContainer);
        AttachArmorToContainer(weaponFrontElement, weaponFrontContainer);
        AttachArmorToContainer(weaponBackElement, weaponBackContainer);

        baseElement.transform.SetParent(vehicle.transform, false);
        baseElement.transform.localPosition = new Vector3(0, baseElement.transform.localPosition.y, baseElement.transform.localPosition.z);

        bodyElement.transform.SetParent(vehicle.transform, false);
        bodyElement.transform.localPosition = new Vector3(0, bodyElement.transform.localPosition.y, bodyElement.transform.localPosition.z);
    }

    private void AttachArmorToContainer(GameObject armor, Transform container)
    {
        if (armor == null)
            return;

        if (container == null)
        {
            // Fallback: keep armor attached to body so it does not remain unparented in root.
            armor.transform.SetParent(bodyElement != null ? bodyElement.transform : null, false);
            return;
        }

        armor.transform.SetParent(container, false);
        armor.transform.rotation = container.rotation;
        armor.transform.localScale = Vector3.Scale(container.localScale, Vector3.one);
    }

    private static Transform ResolveAnchor(Transform root, string anchorName)
    {
        if (root == null || string.IsNullOrEmpty(anchorName))
            return null;

        var direct = root.Find(anchorName);
        if (direct != null)
            return direct;

        var allTransforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == anchorName)
                return allTransforms[i];
        }

        var anchorObjects = root.GetComponentsInChildren<AnchorObject>(true);
        for (int i = 0; i < anchorObjects.Length; i++)
        {
            var anchorObject = anchorObjects[i];
            if (anchorObject != null && anchorObject.anchorName == anchorName)
                return anchorObject.transform;
        }

        return null;
    }

    public void ClearRuntimeParts()
    {
        ClearVehicleChildrenKeepingComposer();

        baseElement = null;
        bodyElement = null;
        weaponLeftElement = null;
        weaponRightElement = null;
        weaponFrontElement = null;
        weaponBackElement = null;
    }

    private void ClearVehicleChildrenKeepingComposer()
    {
        if (vehicle == null)
            return;

        // Keep the helper object that hosts this Composer component.
        Transform keep = transform;
        Transform vehicleTransform = vehicle.transform;

        for (int i = vehicleTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = vehicleTransform.GetChild(i);
            if (child == keep)
                continue;

            Destroy(child.gameObject);
        }
    }
}
