using System.Collections.Generic;
using UnityEngine;

public class WeaponHud
{
    public string weaponPart;
    public SliceFillController sliceFill;
    public float cooldownTime = 0f;

    public WeaponHud(string weaponPart, SliceFillController sliceFill, float cooldownTime)
    {
        this.weaponPart = weaponPart;
        this.sliceFill = sliceFill;
        this.cooldownTime = cooldownTime;
    }
}

public class PlayerHudManager : MonoBehaviour
{
    private class WeaponSliceItem
    {
        public string sliceContainer;
        public string weaponKey;
        public string anchorName;

        public WeaponSliceItem(string sliceContainer, string weaponKey, string anchorName)
        {
            this.sliceContainer = sliceContainer;
            this.weaponKey = weaponKey;
            this.anchorName = anchorName;
        }
    }

    private Dictionary<string, WeaponSliceItem> weaponSliceMap = new Dictionary<string, WeaponSliceItem>{
        {"front", new WeaponSliceItem("WeaponSelectorTop", VehicleElementsKeys.WeaponFront, AnchorNames.WeaponFrontAnchor)},
        {"back", new WeaponSliceItem("WeaponSelectorBottom", VehicleElementsKeys.WeaponBack, AnchorNames.WeaponBackAnchor)},
        {"left", new WeaponSliceItem("WeaponSelectorLeft", VehicleElementsKeys.WeaponLeft, AnchorNames.WeaponLeftAnchor)},
        {"right", new WeaponSliceItem("WeaponSelectorRight", VehicleElementsKeys.WeaponRight, AnchorNames.WeaponRightAnchor)}
        };

    private GameObject hudElement;

    public Dictionary<string, WeaponHud> weaponSelectors = new Dictionary<string, WeaponHud>();

    Dictionary<string, VehicleEntry> savedVehicle = new Dictionary<string, VehicleEntry>();

    public static PlayerHudManager Instance { get; private set; }


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetHudElement(GameObject newHudElement)
    {
        hudElement = newHudElement;
    }

    public void InitPlaySceneHud()
    {
        weaponSelectors.Clear();
        savedVehicle = VehicleManager.Instance.LoadVehicleConfig();
        GameObject parent = GameObject.Find("Hud").FindChildWithName("Dpad");
        VehiclePrefabRegistry registry = GameConfig.VehiclePrefabRegistry;

        foreach (string key in weaponSliceMap.Keys)
        {
            WeaponSliceItem weaponSlice = weaponSliceMap[key];
            SliceFillController sliceFill = parent.FindChildWithName(weaponSlice.sliceContainer)?.GetComponent<SliceFillController>();
            if (sliceFill == null) continue;

            sliceFill.FlashColor = GameConfig.UiConfig.weaponSelectorFlashColor;

            // Una slot senza arma non è presente nel dizionario: cooldown 0, nessuna arma.
            VehicleEntry equipped = savedVehicle.GetValueOrDefault(weaponSlice.weaponKey);
            VehicleWeaponEntry weapon = equipped != null ? registry.GetWeapon(equipped.key) : null;
            float cooldown = weapon != null ? weapon.cooldown : 0f;

            weaponSelectors.Add(key, new WeaponHud(weaponSlice.anchorName, sliceFill, cooldown));
        }
    }

    public Dictionary<string, WeaponHud> GetWeaponsHud()
    {
        return weaponSelectors;
    }
}
