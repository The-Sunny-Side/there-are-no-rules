using UnityEngine;

public class RotateAnimator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private bool clockwise = true;

    public enum RotationAxis { X, Y, Z }
    [SerializeField] private RotationAxis axis = RotationAxis.Z;

    private float direction;

    private void Awake()
    {
        direction = clockwise ? -1f : 1f;
    }

    private void Update()
    {
        float delta = direction * rotationSpeed * Time.deltaTime;

        Vector3 rotation = axis switch
        {
            RotationAxis.X => new Vector3(delta, 0f, 0f),
            RotationAxis.Y => new Vector3(0f, delta, 0f),
            _ => new Vector3(0f, 0f, delta),
        };

        transform.Rotate(rotation);
    }
}