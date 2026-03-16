using UnityEngine;

public class VehicleWeaponPart : MonoBehaviour
{

    public VehicleWeapon mainWeapon;

    private void Awake()
    {
        ResolveMainWeapon();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveMainWeapon();
    }
#endif

    public void SetWeaponHitState(int state)
    {
        if (!ResolveMainWeapon())
        {
            return;
        }

        mainWeapon.SetWeaponHitState(state);
    }

    public void SetMainWeapon(VehicleWeapon weapon)
    {
        mainWeapon = weapon;
    }

    public bool IsHitting()
    {
        if (!ResolveMainWeapon()) return false;

        return mainWeapon.isHitting;
    }

    private bool ResolveMainWeapon()
    {
        if (mainWeapon) return true;

        mainWeapon = GetComponent<VehicleWeapon>();
        if (mainWeapon) return true;

        mainWeapon = GetComponentInParent<VehicleWeapon>();
        if (mainWeapon) return true;

        mainWeapon = GetComponentInChildren<VehicleWeapon>();
        return mainWeapon;
    }
}
