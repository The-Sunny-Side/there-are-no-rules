using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

public class FloatingOnScreenStick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private float moveRange = 50f;
    [SerializeField] private float deadzonePixels = 10f;
    [SerializeField] private RectTransform knobVisual;

    [InputControl(layout = "Vector2")]
    [SerializeField] private new string controlPath;

    protected override string controlPathInternal
    {
        get => controlPath;
        set => controlPath = value;
    }

    private Canvas _canvas;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
    }

    private void HandleInput(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        float scaleFactor = _canvas.scaleFactor;
        localPoint /= scaleFactor;

        if (localPoint.magnitude < deadzonePixels)
        {
            knobVisual.anchoredPosition = Vector2.zero;
            SendValueToControl(Vector2.zero);
            return;
        }

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, moveRange);
        knobVisual.anchoredPosition = clamped;
        SendValueToControl(clamped / moveRange);
    }

    public void OnPointerDown(PointerEventData eventData) => HandleInput(eventData);
    public void OnDrag(PointerEventData eventData) => HandleInput(eventData);

    public void OnPointerUp(PointerEventData eventData)
    {
        knobVisual.anchoredPosition = Vector2.zero;
        SendValueToControl(Vector2.zero);
    }
}