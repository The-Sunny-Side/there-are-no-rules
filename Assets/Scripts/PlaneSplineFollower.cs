using UnityEngine;
using UnityEngine.Splines;

public class PlaneSplineFollower : MonoBehaviour
{
    [Header("Spline")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Movement")]
    [SerializeField] private float speed = 0.03f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 5f;

    [SerializeField]
    private Vector3 rotationOffset;

    private float progress;

    private void Update()
    {
        if (splineContainer == null)
            return;

        progress += speed * Time.deltaTime;

        if (progress > 1f)
            progress -= 1f;

        Vector3 position =
            splineContainer.EvaluatePosition(progress);

        Vector3 tangent =
            splineContainer.EvaluateTangent(progress);

        transform.position = position;

        if (tangent.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(tangent.normalized);

            Quaternion finalRotation = targetRotation * Quaternion.Euler(rotationOffset);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                finalRotation,
                rotationSpeed * Time.deltaTime);
        }
    }
}