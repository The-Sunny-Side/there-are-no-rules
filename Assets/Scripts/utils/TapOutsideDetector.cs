using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class TapOutsideDetector : MonoBehaviour
{
    [SerializeField] private UnityEvent onTapOutside;
    [SerializeField] private RectTransform[] excludedAreas;
    private RectTransform _rect;

    private void Awake()
    {
        _rect = transform as RectTransform;
    }

    private void Update()
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                    CheckTap(touch.position.ReadValue());
            }
        }
        else if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                CheckTap(Mouse.current.position.ReadValue());
        }
    }

    private void CheckTap(Vector2 screenPos)
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(_rect, screenPos, null))
            return;

        foreach (var area in excludedAreas)
        {
            if (area != null && RectTransformUtility.RectangleContainsScreenPoint(area, screenPos, null))
                return;
        }

        onTapOutside?.Invoke();
    }
}