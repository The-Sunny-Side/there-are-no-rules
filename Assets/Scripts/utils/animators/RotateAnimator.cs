using UnityEngine;

public class RotateAnimator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 180f; // gradi al secondo
    [SerializeField] private bool clockwise = true;

    private float direction;

    private void Awake()
    {
        direction = clockwise ? -1f : 1f;
    }

    private void Update()
    {
        // Rotazione costante, indipendente dal frame rate
        transform.Rotate(0f, 0f, direction * rotationSpeed * Time.deltaTime);
    }
}
