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

    private void OnTriggerEnter(Collider collider)
    {
        VehicleWeaponPart weaponPart = collider.gameObject.GetComponent<VehicleWeaponPart>();

        if (weaponPart && weaponPart.IsHitting())
        {
            _pendingHit = true;

            _pendingPushDirection = (transform.position - collider.transform.position).normalized;
        }
    }
}