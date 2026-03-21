using PurrNet.Prediction;
using UnityEngine;

public class CollisionDetector : PredictedIdentity<CollisionDetector.State>
{
    [SerializeField] private float pushForce = 10f;
    private PredictedRigidbody _rigidbody;

    private bool _pendingHit;
    private Vector3 _pendingPushDirection;

    void Awake()
    {
        _rigidbody = GetComponent<PredictedRigidbody>();
    }

    public struct State : IPredictedData<State>
    {
        public bool hitted;
        public Vector3 pushDirection;

        public void Dispose() { }
    }

    protected override void Simulate(ref State state, float delta)
    {
        state.hitted = _pendingHit;
        state.pushDirection = _pendingPushDirection;

        if (state.hitted)
        {
            
            Vector3 dir = state.pushDirection.normalized;

            _rigidbody.AddForce(dir * pushForce, ForceMode.Impulse);
        }

        _pendingHit = false;
        _pendingPushDirection = Vector3.zero;
    }

    private void OnCollisionEnter(Collision col)
    {
        HandleWeaponContact(col.collider, "OnCollisionEnter");
    }

    private void HandleWeaponContact(Collider otherCollider, string source)
    {
        if (!otherCollider)
            return;

        VehicleWeaponPart weaponPart = otherCollider.GetComponent<VehicleWeaponPart>();
        if (!weaponPart)
            weaponPart = otherCollider.GetComponentInParent<VehicleWeaponPart>();

        if (!weaponPart)
            return;

        if (weaponPart.transform.root == transform.root)
            return;

        Debug.Log($"{source}: {otherCollider.gameObject.name} is weapon: true");

        if (!weaponPart.IsHitting())
            return;

        Vector3 hitPoint = otherCollider.ClosestPoint(transform.position);


        _pendingHit = true;
        _pendingPushDirection = (transform.position - hitPoint).normalized;

        weaponPart.SetWeaponHitState(0);
        Debug.Log($"Hit from {otherCollider.gameObject.name}, applying push force in direction {_pendingPushDirection}");
    }
}
