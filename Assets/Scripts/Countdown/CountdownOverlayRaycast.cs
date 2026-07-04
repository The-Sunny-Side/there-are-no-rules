using UnityEngine;
using UnityEngine.UI;

public class CountdownOverlayRaycast : Image
{
    private RectTransform _excludedTarget;
    private float _excludeRadiusExtra;

    public void SetExcluded(RectTransform target, float radiusExtra = 12f)
    {
        _excludedTarget = target;
        _excludeRadiusExtra = radiusExtra;
    }

    public void ClearExcluded()
    {
        _excludedTarget = null;
    }

    public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (_excludedTarget == null)
            return true;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _excludedTarget, screenPoint, eventCamera, out Vector2 local);

        float radius = Mathf.Max(
            _excludedTarget.rect.width,
            _excludedTarget.rect.height) * 0.5f + _excludeRadiusExtra;

        return local.magnitude > radius;
    }
}