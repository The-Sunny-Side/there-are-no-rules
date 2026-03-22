using PurrNet;
using UnityEngine;

public class PivotWeaponBehaviour : MonoBehaviour
{
    [SerializeField] private Transform rotationPivot;
    [SerializeField] private bool availableRotation = true;
    [Min(0f)]
    [SerializeField] private float rotationLerpSpeed = 8f;
    [Min(0f)]
    [SerializeField] private float detectionRadius = 20f;
    [Range(1f, 180f)]
    [SerializeField] private float coneAngle = 25f;
    [SerializeField] private LayerMask PlayerMask;

    private Quaternion defaultLocalRotation;
    private NetworkIdentity _networkIdentity;
    private Collider _currentHighlightTarget;

    private static int _outlineLayer = -1;
    private int _cachedOriginalLayer;

    private void Awake()
    {
        if (_outlineLayer < 0)
            _outlineLayer = LayerMask.NameToLayer("OutlineObjects");
        AutoAssignRotationPivot();
        CacheDefaultRotation();
    }

    private void OnValidate()
    {
        AutoAssignRotationPivot();
    }

    private void Update()
    {
        if (rotationPivot == null) return;

        if (!availableRotation)
        {
            ReturnPivotToDefault();
            DrawSearchCone();
            SetHighlightTarget(null);
            return;
        }

        bool foundTarget = DetectTarget(out Vector3 targetPoint, out Collider targetCollider);

        if (foundTarget)
        {
            DrawTargetLine(targetPoint);
            RotatePivotTowards(targetPoint);
            SetHighlightTarget(targetCollider);
        }
        else
        {
            DrawSearchCone();
            ReturnPivotToDefault();
            SetHighlightTarget(null);
        }
    }

    private void SetHighlightTarget(Collider newTarget)
    {
        _networkIdentity ??= GetComponentInParent<NetworkIdentity>();

        if (_networkIdentity != null && !_networkIdentity.isOwner)
            return;

        if (newTarget == _currentHighlightTarget) return;

        if (_currentHighlightTarget != null)
            ApplyHighlight(_currentHighlightTarget, false);

        _currentHighlightTarget = newTarget;

        if (_currentHighlightTarget != null)
            ApplyHighlight(_currentHighlightTarget, true);
    }

    private void ApplyHighlight(Collider col, bool highlighted)
    {
        if (_outlineLayer < 0) return;

        GameObject root = col.gameObject;
        if (highlighted)
        {
            _cachedOriginalLayer = root.layer;
            SetLayerRecursively(root, _outlineLayer);
        }
        else
        {
            SetLayerRecursively(root, _cachedOriginalLayer);
        }
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void OnDisable()
    {
        if (_currentHighlightTarget != null)
        {
            ApplyHighlight(_currentHighlightTarget, false);
            _currentHighlightTarget = null;
        }
    }

    private bool DetectTarget(out Vector3 targetPoint, out Collider targetCollider)
    {
        targetPoint = Vector3.zero;
        targetCollider = null;

        Collider[] hits = Physics.OverlapSphere(
            transform.position, detectionRadius, PlayerMask, QueryTriggerInteraction.Ignore);

        float closestDistance = float.MaxValue;
        bool found = false;

        foreach (Collider col in hits)
        {
            Vector3 toTarget = col.bounds.center - transform.position;
            if (Vector3.Angle(transform.forward, toTarget) > coneAngle) continue;

            float dist = toTarget.magnitude;
            if (dist < closestDistance)
            {
                closestDistance = dist;
                targetPoint = col.bounds.center;
                targetCollider = col;
                found = true;
            }
        }

        return found;
    }

    private void DrawSearchCone()
    {
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;

        float coneRad = coneAngle * Mathf.Deg2Rad;
        float cosAngle = Mathf.Cos(coneRad);
        float sinAngle = Mathf.Sin(coneRad);

        const int perimeterRays = 16;
        Vector3 prevEdge = Vector3.zero;

        for (int i = 0; i <= perimeterRays; i++)
        {
            float a = (360f / perimeterRays * i) * Mathf.Deg2Rad;
            Vector3 radial = right * Mathf.Cos(a) + up * Mathf.Sin(a);
            Vector3 edgeDir = (forward * cosAngle + radial * sinAngle).normalized;
            Vector3 edgePoint = origin + edgeDir * detectionRadius;

            Debug.DrawRay(origin, edgeDir * detectionRadius, Color.red);

            if (i > 0)
                Debug.DrawLine(prevEdge, edgePoint, Color.red);

            prevEdge = edgePoint;
        }

        Debug.DrawRay(origin, forward * detectionRadius, Color.red);
    }

    private void DrawTargetLine(Vector3 targetPoint)
    {
        Vector3 dir = targetPoint - transform.position;
        if (dir.sqrMagnitude > Mathf.Epsilon)
            Debug.DrawRay(transform.position, dir, Color.yellow);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            DrawSearchConeGizmos();
    }

    private void DrawSearchConeGizmos()
    {
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;

        float coneRad = coneAngle * Mathf.Deg2Rad;
        float cosAngle = Mathf.Cos(coneRad);
        float sinAngle = Mathf.Sin(coneRad);

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        const int perimeterRays = 16;
        Vector3 prevEdge = Vector3.zero;

        for (int i = 0; i <= perimeterRays; i++)
        {
            float a = (360f / perimeterRays * i) * Mathf.Deg2Rad;
            Vector3 radial = right * Mathf.Cos(a) + up * Mathf.Sin(a);
            Vector3 edgeDir = (forward * cosAngle + radial * sinAngle).normalized;
            Vector3 edgePoint = origin + edgeDir * detectionRadius;

            Gizmos.DrawLine(origin, edgePoint);

            if (i > 0)
                Gizmos.DrawLine(prevEdge, edgePoint);

            prevEdge = edgePoint;
        }

        Gizmos.DrawLine(origin, origin + forward * detectionRadius);
    }

    private void RotatePivotTowards(Vector3 targetPoint)
    {
        Vector3 directionToTarget = targetPoint - rotationPivot.position;
        if (directionToTarget.sqrMagnitude <= Mathf.Epsilon) return;

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized, transform.up);
        Quaternion targetLocalRotation = GetLocalRotation(targetRotation);
        float lerpFactor = rotationLerpSpeed * Time.deltaTime;

        rotationPivot.localRotation = Quaternion.Slerp(
            rotationPivot.localRotation,
            targetLocalRotation,
            lerpFactor);
    }

    private void ReturnPivotToDefault()
    {
        float lerpFactor = rotationLerpSpeed * Time.deltaTime;
        rotationPivot.localRotation = Quaternion.Slerp(
            rotationPivot.localRotation,
            defaultLocalRotation,
            lerpFactor);
    }

    private void AutoAssignRotationPivot()
    {
        Transform childPivot = FindChildPivot();
        if (childPivot != null)
        {
            if (rotationPivot == null || rotationPivot == transform)
                rotationPivot = childPivot;
            return;
        }

        if (rotationPivot == null)
            rotationPivot = transform;
    }

    private Transform FindChildPivot()
    {
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("pivot"))
                return child;
        }
        return null;
    }

    private void CacheDefaultRotation()
    {
        if (rotationPivot != null)
            defaultLocalRotation = rotationPivot.localRotation;
    }

    private Quaternion GetLocalRotation(Quaternion worldRotation)
    {
        if (rotationPivot.parent == null)
            return worldRotation;
        return Quaternion.Inverse(rotationPivot.parent.rotation) * worldRotation;
    }
}
