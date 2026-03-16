using PurrNet.Prediction;
using UnityEngine;

public class HitBehaviour : MonoBehaviour
{
    [SerializeField] private float pushForce = 1000f;
    private VehicleWeaponPart _weaponPart;

    void Awake()
    {
        _weaponPart = GetComponent<VehicleWeaponPart>();
        if (!_weaponPart)
        {
            _weaponPart = GetComponentInParent<VehicleWeaponPart>();
        }
    }

    private void OnTriggerEnter(Collider col)
    {
        HandleWeaponContact(col, "OnTriggerEnter");
    }

    private void HandleWeaponContact(Collider otherCollider, string source)
    {

        if (!otherCollider.CompareTag("PlayerSphere")) return;
        if (!_weaponPart || !_weaponPart.IsHitting()) return;

        PredictedRigidbody hittedBody = otherCollider.GetComponent<PredictedRigidbody>();
        if (!hittedBody)
        {
            hittedBody = otherCollider.GetComponentInParent<PredictedRigidbody>();
        }
        if (!hittedBody) return;

        Vector3 hitPoint = otherCollider.ClosestPoint(transform.position);
        Vector3 pushDirection = (otherCollider.transform.position - hitPoint).normalized;
        if (pushDirection.sqrMagnitude < 0.0001f)
        {
            pushDirection = (otherCollider.transform.position - transform.position).normalized;
        }

        hittedBody.AddForce(pushDirection * pushForce, ForceMode.Impulse);
        _weaponPart.SetWeaponHitState(0);
        Debug.Log($"{source}: hit from {otherCollider.gameObject.name}, applying push force in direction {pushDirection}");
    }
}
