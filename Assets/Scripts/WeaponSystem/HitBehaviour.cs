using PurrNet.Prediction;
using UnityEngine;

public class HitBehaviour : MonoBehaviour
{
    [SerializeField] private float pushForce = 1000f;

    private VehicleWeaponPart _weaponPart;
    private int _playerLayer = -1;

    void Awake()
    {
        _playerLayer = LayerMask.NameToLayer("Player");
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
        if (!_weaponPart || !_weaponPart.IsHitting()) return;

        if (!TryGetHitBody(otherCollider, out PredictedRigidbody hittedBody)) return;

        Vector3 hitPoint = otherCollider.ClosestPoint(transform.position);
        Vector3 pushDirection = (otherCollider.transform.position - hitPoint).normalized;
        if (pushDirection.sqrMagnitude < 0.0001f)
        {
            pushDirection = (otherCollider.transform.position - transform.position).normalized;
        }
        EffectsManager.Instance.SpawnBalloon(hitPoint);

        hittedBody.AddForce(pushDirection * pushForce, ForceMode.Impulse);
        _weaponPart.SetWeaponHitState(0);
        Debug.Log($"{source}: hit from {otherCollider.gameObject.name}, applying push force in direction {pushDirection}");
    }

    private bool TryGetHitBody(Collider otherCollider, out PredictedRigidbody hittedBody)
    {
        hittedBody = null;

        var player = otherCollider.GetComponentInParent<PlayerIdentity>();
        if (player != null)
        {
            if (player.transform.root == transform.root) return false;

            hittedBody = player.PredictedRigidbody;
            return hittedBody != null;
        }

        if (!IsOnPlayerLayer(otherCollider)) return false;

        hittedBody = GetPredictedRigidbody(otherCollider);
        if (hittedBody == null) return false;

        return hittedBody.transform.root != transform.root;
    }

    private bool IsOnPlayerLayer(Collider otherCollider)
    {
        if (_playerLayer < 0) return false;
        if (otherCollider.gameObject.layer == _playerLayer) return true;

        return otherCollider.attachedRigidbody != null &&
               otherCollider.attachedRigidbody.gameObject.layer == _playerLayer;
    }

    private static PredictedRigidbody GetPredictedRigidbody(Collider otherCollider)
    {
        if (otherCollider.TryGetComponent(out PredictedRigidbody predictedRigidbody))
            return predictedRigidbody;

        if (otherCollider.attachedRigidbody != null &&
            otherCollider.attachedRigidbody.TryGetComponent(out predictedRigidbody))
            return predictedRigidbody;

        return otherCollider.GetComponentInParent<PredictedRigidbody>();
    }
}
