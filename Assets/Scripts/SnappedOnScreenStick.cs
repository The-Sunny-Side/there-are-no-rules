using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

public class SnappedOnScreenStick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private float deadzonePixels = 20f;
    [SerializeField] private float snapDistance = 40f;
    [SerializeField] private RectTransform knobVisual;

    [InputControl(layout = "Vector2")]
    [SerializeField] private new string controlPath;
    [SerializeField] private Image[] weaponIcons;

    private void Awake()
    {
        Dictionary<string, VehicleElement> data  = VehicleManager.Instance?.LoadVehicleConfig();

        if(data != null)
        {
            weaponIcons[0].sprite = data[VehicleElementsKeys.WeaponFront].icon;
            weaponIcons[1].sprite = data[VehicleElementsKeys.WeaponLeft].icon;
            weaponIcons[2].sprite = data[VehicleElementsKeys.WeaponBack].icon;
            weaponIcons[3].sprite = data[VehicleElementsKeys.WeaponRight].icon;
        }
    }

    protected override string controlPathInternal
    {
        get => controlPath;
        set => controlPath = value;
    }

    private Vector2 _pointerStartPos;

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerStartPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - _pointerStartPos;

        if (delta.magnitude < deadzonePixels)
        {
            knobVisual.anchoredPosition = Vector2.zero;
            SendValueToControl(Vector2.zero);
            return;
        }

        Vector2 snapped = SnapToCardinal(delta.normalized);
        knobVisual.anchoredPosition = snapped * snapDistance;
        SendValueToControl(snapped);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        knobVisual.anchoredPosition = Vector2.zero;
        SendValueToControl(Vector2.zero);
    }

    private Vector2 SnapToCardinal(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return input.x > 0 ? Vector2.right : Vector2.left;
        else
            return input.y > 0 ? Vector2.up : Vector2.down;
    }
}