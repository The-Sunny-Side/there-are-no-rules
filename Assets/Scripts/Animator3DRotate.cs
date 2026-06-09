using UnityEngine;

public class Animator3DRotate : MonoBehaviour
{
    public enum RotationDirection
    {
        Clockwise = 1,
        CounterClockwise = -1
    }

    [Header("Rotation")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private RotationDirection direction = RotationDirection.Clockwise;

    [Header("Floating")]
    [SerializeField] private bool enableFloating = true;
    [SerializeField] private float floatAmplitude = 0.25f;
    [SerializeField] private float floatFrequency = 1.5f;

    [Header("Scaling")]
    [SerializeField] private bool enableScaling = false;
    [SerializeField] private float scaleAmplitude = 0.1f;
    [SerializeField] private float scaleFrequency = 1.5f;

    private Vector3 initialPosition;
    private Vector3 initialScale;

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialScale = transform.localScale;
    }

    private void Update()
    {
        float time = Time.time;

        // Rotazione
        if (enableRotation)
        {
            float speed = rotationSpeed * (int)direction;

            transform.Rotate(
                rotationAxis.normalized,
                speed * Time.deltaTime,
                Space.Self
            );
        }

        // Oscillazione verticale
        if (enableFloating)
        {
            Vector3 pos = initialPosition;
            pos.y += Mathf.Sin(time * floatFrequency) * floatAmplitude;
            transform.localPosition = pos;
        }

        // Pulsazione scala
        if (enableScaling)
        {
            float scaleOffset =
                Mathf.Sin(time * scaleFrequency) * scaleAmplitude;

            transform.localScale =
                initialScale * (1f + scaleOffset);
        }
    }
}
