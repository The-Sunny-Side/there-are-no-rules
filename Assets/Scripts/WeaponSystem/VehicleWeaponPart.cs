using UnityEngine;

public class VehicleWeaponPart : MonoBehaviour
{

    public VehicleWeapon mainWeapon;

    public void SetWeaponHitState(int state)
    {
        mainWeapon.SetWeaponHitState(state);
    }

    public void SetMainWeapon(VehicleWeapon weapon)
    {
        mainWeapon = weapon;
    }

    public bool IsHitting()
    {
        if (mainWeapon)
        {
            return mainWeapon.isHitting;
        }

        return false;
    }
}
