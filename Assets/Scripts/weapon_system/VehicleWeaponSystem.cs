using System.Collections.Generic;
using UnityEngine;

public class VehicleWeaponSystem : MonoBehaviour
{

    private MobileInputManager inputManager;
    private Dictionary<string, string> weaponsKey = new Dictionary<string, string>();

    void Start()
    {
        inputManager = MobileInputManager.instance;
        weaponsKey.Add("front", AnchorNames.WeaponFrontAnchor);
        weaponsKey.Add("back", AnchorNames.WeaponBackAnchor);
        weaponsKey.Add("left", AnchorNames.WeaponLeftAnchor);
        weaponsKey.Add("right", AnchorNames.WeaponRightAnchor);
    }

    public void ActivateWeapon(string direction) {         
        if (weaponsKey.TryGetValue(direction, out string partName))
        {
            VehicleWeapon weapon = gameObject.FindChildWithName(partName)?.FindChildWithTag("Weapon")?.GetComponent<VehicleWeapon>();
            if (weapon != null)
            {
                weapon.ActivateWeapon();
            }
        }
    }

    void Update()
    {
        if (inputManager != null)
        {
            if (inputManager.slideUpHeld)
            {
                ActivateWeapon("front");
            }

            if (inputManager.slideDownHeld)
            {
                ActivateWeapon("back");
            }

            if (inputManager.slideLeftHeld)
            {
                ActivateWeapon("left");
            }

            if (inputManager.slideRightHeld)
            {
                ActivateWeapon("right");
            }
        }
    }
}
