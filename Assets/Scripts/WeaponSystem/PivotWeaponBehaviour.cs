using UnityEngine;

public class PivotWeaponBehaviour : MonoBehaviour
{
    [SerializeField] private Transform pivotFront;
    [Min(0f)]
    [SerializeField] private float rotationLerpSpeed = 8f;
    [Min(1)]
    [SerializeField] private int raysCount = 8;
    [Min(1)]
    [SerializeField] private int ringsCount = 3;
    [Min(0f)]
    [SerializeField] private float raySpread = 0.25f;
    [Min(0f)]
    [SerializeField] private float RayLenght = .5f;
    [SerializeField] private LayerMask PlayerMask;

    private Quaternion defaultLocalRotation;

    private void Awake()
    {
        AutoAssignPivotFront();
        CacheDefaultRotation();
    }

    private void OnValidate()
    {
        AutoAssignPivotFront();
    }

    private void Update()
    {
        Vector3 origin = transform.position;
        CastRaysAndAling(origin, out Vector3 targetPoint, out bool foundTarget);

        if (foundTarget)
        {
            DrawDirectionRay(origin, targetPoint);
        }
        else
        {
            DrawSearchRays(origin);
        }

        if (pivotFront == null)
        {
            return;
        }

        if (foundTarget)
        {
            RotatePivotTowards(targetPoint);
        }
        else
        {
            ReturnPivotToDefault();
        }
    }

    private void CastRaysAndAling(Vector3 origin, out Vector3 targetPoint, out bool foundTarget)
    {
        Vector3 direction = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;
        int safeRaysCount = Mathf.Max(1, raysCount);
        int safeRingsCount = Mathf.Max(1, ringsCount);
        float closestHitDistance = float.MaxValue;

        targetPoint = Vector3.zero;
        foundTarget = false;

        EvaluateRay(origin, direction, ref foundTarget, ref closestHitDistance, ref targetPoint);

        for (int ring = 1; ring <= safeRingsCount; ring++)
        {
            float ringPercent = (float)ring / safeRingsCount;
            float currentSpread = raySpread * ringPercent;
            int raysInRing = Mathf.Max(1, Mathf.RoundToInt(safeRaysCount * ringPercent));
            float angleStep = 360f / raysInRing;
            float angleOffset = ring % 2 == 0 ? angleStep * 0.5f : 0f;

            for (int i = 0; i < raysInRing; i++)
            {
                float angle = (angleOffset + angleStep * i) * Mathf.Deg2Rad;

                Vector3 radial =
                    right * Mathf.Cos(angle) +
                    up * Mathf.Sin(angle);
                Vector3 dir = (direction + radial * currentSpread).normalized;

                EvaluateRay(origin, dir, ref foundTarget, ref closestHitDistance, ref targetPoint);
            }
        }
    }

    private void EvaluateRay(
        Vector3 origin,
        Vector3 dir,
        ref bool foundTarget,
        ref float closestHitDistance,
        ref Vector3 targetPoint)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, RayLenght, PlayerMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.distance < closestHitDistance)
            {
                closestHitDistance = hit.distance;
                targetPoint = hit.collider.bounds.center;
                foundTarget = true;
            }
        }
    }

    private void DrawSearchRays(Vector3 origin)
    {
        Vector3 direction = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;
        int safeRaysCount = Mathf.Max(1, raysCount);
        int safeRingsCount = Mathf.Max(1, ringsCount);

        Debug.DrawRay(origin, direction * RayLenght, Color.red);

        for (int ring = 1; ring <= safeRingsCount; ring++)
        {
            float ringPercent = (float)ring / safeRingsCount;
            float currentSpread = raySpread * ringPercent;
            int raysInRing = Mathf.Max(1, Mathf.RoundToInt(safeRaysCount * ringPercent));
            float angleStep = 360f / raysInRing;
            float angleOffset = ring % 2 == 0 ? angleStep * 0.5f : 0f;

            for (int i = 0; i < raysInRing; i++)
            {
                float angle = (angleOffset + angleStep * i) * Mathf.Deg2Rad;
                Vector3 radial =
                    right * Mathf.Cos(angle) +
                    up * Mathf.Sin(angle);
                Vector3 dir = (direction + radial * currentSpread).normalized;

                Debug.DrawRay(origin, dir * RayLenght, Color.red);
            }
        }
    }

    private void DrawDirectionRay(Vector3 origin, Vector3 targetPoint)
    {
        Vector3 directionToTarget = targetPoint - origin;
        if (directionToTarget.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Debug.DrawRay(origin, directionToTarget, Color.yellow);
    }

    private void RotatePivotTowards(Vector3 targetPoint)
    {
        Vector3 directionToTarget = targetPoint - pivotFront.position;
        if (directionToTarget.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized, transform.up);
        Quaternion targetLocalRotation = GetLocalRotation(targetRotation);
        float lerpFactor = rotationLerpSpeed * Time.deltaTime;

        pivotFront.localRotation = Quaternion.Slerp(
            pivotFront.localRotation,
            targetLocalRotation,
            lerpFactor);
    }

    private void ReturnPivotToDefault()
    {
        float lerpFactor = rotationLerpSpeed * Time.deltaTime;
        pivotFront.localRotation = Quaternion.Slerp(
            pivotFront.localRotation,
            defaultLocalRotation,
            lerpFactor);
    }

    private void AutoAssignPivotFront()
    {
        if (pivotFront != null)
        {
            return;
        }

        Transform childPivot = transform.Find("pivotFront");
        if (childPivot != null)
        {
            pivotFront = childPivot;
        }
    }

    private void CacheDefaultRotation()
    {
        if (pivotFront != null)
        {
            defaultLocalRotation = pivotFront.localRotation;
        }
    }

    private Quaternion GetLocalRotation(Quaternion worldRotation)
    {
        if (pivotFront.parent == null)
        {
            return worldRotation;
        }

        return Quaternion.Inverse(pivotFront.parent.rotation) * worldRotation;
    }
}
