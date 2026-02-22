using UnityEngine;

public class VehicleWeaponPart : MonoBehaviour
{

    public VehicleWeapon mainWeapon;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

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
