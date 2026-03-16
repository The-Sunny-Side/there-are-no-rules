using UnityEngine;

public class VehicleWeapon : MonoBehaviour
{
    public bool isHitting = false;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void ActivateWeapon()
    {
        if (_animator != null && !isHitting)
        {
            _animator.SetTrigger("activate");
        }
    }

    public void SetWeaponHitState(int state)
    {
        isHitting = state == 1;
    }
}
