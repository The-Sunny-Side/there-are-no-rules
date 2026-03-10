using PurrNet.Prediction;
using UnityEngine;

public class HitBehaviour : PredictedIdentity<HitBehaviour.State>
{
    [SerializeField] private float pushForce = 1000f;
    private BoxCollider _boxCollider;
    private VehicleWeaponPart _weaponPart;

    private bool _pendingHit;
    private Vector3 _pendingPushDirection;
    private PredictedRigidbody _hittedBody;

    void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();
        _weaponPart = GetComponent<VehicleWeaponPart>();
        if (!_weaponPart)
        {
            _weaponPart = GetComponentInParent<VehicleWeaponPart>();
        }
    }

    private void FixedUpdate()
    {
        UpdateTriggerState();
    }

    public struct State : IPredictedData<State>
    {
        public bool hitted;
        public Vector3 pushDirection;
        public PredictedRigidbody hittedBody;

        public void Dispose() { }
    }

   

    protected override void Simulate(ref State state, float delta)
    {
        UpdateTriggerState();


        state.hitted = _pendingHit;
        state.pushDirection = _pendingPushDirection;
        state.hittedBody= _hittedBody;
        if (state.hitted && state.hittedBody)
        {

            Vector3 dir = state.pushDirection.normalized;

            state.hittedBody.AddForce(dir * pushForce, ForceMode.Impulse);
        }

        _pendingHit = false;
        _pendingPushDirection = Vector3.zero;
        _hittedBody = null;

    }

    private void UpdateTriggerState()
    {
        if (!_boxCollider || !_weaponPart)
        {
            return;
        }

        // Trigger when weapon is idle/returning/already consumed its hit window.
        _boxCollider.isTrigger = !_weaponPart.IsHitting();
    }

    private void OnCollisionEnter(Collision col)
    {
        HandleWeaponContact(col.collider, "OnCollisionEnter");
    }

    private void HandleWeaponContact(Collider otherCollider, string source)
    {

        if (!otherCollider.CompareTag("Player")) return;
        if (!_weaponPart || !_weaponPart.IsHitting()) return;
        
        Vector3 hitPoint = otherCollider.ClosestPoint(transform.position);

        _hittedBody = otherCollider.gameObject.GetComponent<PredictedRigidbody>();
        if (!_hittedBody) return;

        _pendingHit = true;
        _pendingPushDirection = (transform.position - hitPoint).normalized;
        _weaponPart.SetWeaponHitState(0);
        Debug.Log($"{source}: hit from {otherCollider.gameObject.name}, applying push force in direction {_pendingPushDirection}");
    }
}
