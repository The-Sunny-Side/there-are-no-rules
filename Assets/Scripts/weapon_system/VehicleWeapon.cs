using UnityEngine;

public class VehicleWeapon : MonoBehaviour
{
    public bool isHitting = false;

    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void ActivateWeapon()
    {
        if (animator != null && !isHitting)
        {
            animator.SetTrigger("activate");
        }
    }

    public void SetWeaponHitState(int state)
    {
        isHitting = state==1;
    }
}