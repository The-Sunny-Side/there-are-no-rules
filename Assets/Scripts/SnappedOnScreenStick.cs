using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.EventSystems;

public class SnappedOnScreenStick : OnScreenStick, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private float deadzonePixels = 20f;
    [SerializeField] private float snapDistance = 60f;

    private RectTransform _knobRect;
    private RectTransform _backgroundRect;
    private Vector2 _backgroundCenter;

    protected override void OnEnable()
    {
        base.OnEnable();
        _knobRect = GetComponent<RectTransform>();
        _backgroundRect = _knobRect.parent as RectTransform;

        // Il centro del background in coordinate locali del suo parent
        // dipende dal pivot: pivot (0.5, 0.5) = anchoredPosition è già il centro
        // ma lo calcoliamo esplicitamente per essere sicuri
        _backgroundCenter = _backgroundRect.anchoredPosition
                          + new Vector2(
                              _backgroundRect.rect.width * (0.5f - _backgroundRect.pivot.x),
                              _backgroundRect.rect.height * (0.5f - _backgroundRect.pivot.y)
                            );
    }

    public new void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
    }

    public new void OnDrag(PointerEventData eventData)
    {
        // Converti il touch in coordinate locali del PARENT del background
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
               _backgroundRect.parent as RectTransform,
               eventData.position,
               eventData.pressEventCamera,
               out Vector2 localPoint
           );

        Vector2 delta = localPoint - _backgroundCenter;

        Debug.Log($"touch={eventData.position} | localPoint={localPoint} | center={_backgroundCenter} | delta={delta} | magnitude={delta.magnitude}");

        if (delta.magnitude < deadzonePixels)
        {
            _knobRect.anchoredPosition = Vector2.zero;
            SendValueToControl(Vector2.zero);
            return;
        }

        Vector2 snapped = SnapToCardinal(delta.normalized);
        _knobRect.anchoredPosition = snapped * snapDistance;
        SendValueToControl(snapped);
    }

    public new void OnPointerUp(PointerEventData eventData)
    {
        _knobRect.anchoredPosition = Vector2.zero;
        SendValueToControl(Vector2.zero);
        base.OnPointerUp(eventData);
    }

    private Vector2 SnapToCardinal(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return input.x > 0 ? Vector2.right : Vector2.left;
        else
            return input.y > 0 ? Vector2.up : Vector2.down;
    }
}