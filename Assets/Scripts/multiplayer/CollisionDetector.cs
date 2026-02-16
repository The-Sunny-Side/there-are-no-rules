using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        VehicleWeapon weapon = collider.gameObject.GetComponent<VehicleWeapon>();
        VehicleWeaponPart weaponPart = collider.gameObject.GetComponent<VehicleWeaponPart>();

        if (weaponPart && weaponPart.IsHitting())
        {
            //Colpito
            Debug.Log("collido" + collider.gameObject.name);

        }

        else if (weapon && weapon.isHitting)
        {
            //Colpito
            Debug.Log("collido" + collider.gameObject.name);

        }

        
    }
}
 