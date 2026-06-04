using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TutorialOverlayRaycast))]
public class TutorialOverlayController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform leftStick;

    private static readonly int PropHoleCenter = Shader.PropertyToID("_HoleCenter");
    private static readonly int PropHoleRadius = Shader.PropertyToID("_HoleRadius");
    private static readonly int PropHoleSoftness = Shader.PropertyToID("_HoleSoftness");
    private static readonly int PropGlowColor = Shader.PropertyToID("_GlowColor");
    private static readonly int PropGlowWidth = Shader.PropertyToID("_GlowWidth");
    private static readonly int PropGlowIntensity = Shader.PropertyToID("_GlowIntensity");

    private Material _mat;
    private TutorialOverlayRaycast _raycastOverlay;

    private void Awake()
    {
        _raycastOverlay = GetComponent<TutorialOverlayRaycast>();
        _mat = _raycastOverlay.material = new Material(_raycastOverlay.material);
    }

    public void SetHole(
        RectTransform target,
        float radiusExtra = 0f,
        float softness = 0.03f,
        Color? glowColor = null,
        float glowWidth = 0.03f,
        float glowIntensity = 1.5f)
    {
        Vector2 screenPos = GetScreenCenter(target);
        Vector2 normalized = new Vector2(screenPos.x / Screen.width,
                                         screenPos.y / Screen.height);

        float halfH = GetScreenSize(target).y * 0.5f;
        float radiusNorm = (halfH + radiusExtra) / Screen.height;

        _mat.SetVector(PropHoleCenter, new Vector4(normalized.x, normalized.y, 0, 0));
        _mat.SetFloat(PropHoleRadius, radiusNorm);
        _mat.SetFloat(PropHoleSoftness, softness);
        _mat.SetColor(PropGlowColor, glowColor ?? new Color(0.4f, 1f, 0.95f, 1f));
        _mat.SetFloat(PropGlowWidth, glowWidth);
        _mat.SetFloat(PropGlowIntensity, glowIntensity);

        _raycastOverlay.SetExcluded(target, radiusExtra);
    }

    public void ClearHole()
    {
        _mat.SetFloat(PropHoleRadius, 0f);
        _raycastOverlay.ClearExcluded();
    }

    public void UncoverLeftStick()
    {
        Show();
        SetHole(
            target: leftStick,
            radiusExtra: 12f,
            softness: 0.025f,
            glowColor: new Color(0.4f, 1f, 0.95f),
            glowWidth: 0.04f,
            glowIntensity: 2f);
    }

    public void Show() => GetComponent<UiTransition>().Show();
    public void Hide()
    {
        ClearHole();
        GetComponent<UiTransition>().Hide();
    }

    // --- Utility ---

    private Vector2 GetScreenCenter(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) / 2f;
        return RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldCenter);
    }

    private Vector2 GetScreenSize(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float w = Vector3.Distance(corners[0], corners[3]);
        float h = Vector3.Distance(corners[0], corners[1]);
        return new Vector2(w, h);
    }

    [ContextMenu("Debug Hole Values")]
    public void DebugHoleValues()
    {
        Debug.Log($"HoleCenter: {_mat.GetVector(PropHoleCenter)}");
        Debug.Log($"HoleRadius: {_mat.GetFloat(PropHoleRadius)}");
        Debug.Log($"Screen: {Screen.width}x{Screen.height}");
        Debug.Log($"Material instance ID: {_mat.GetInstanceID()}");
        Debug.Log($"Overlay material instance ID: {_raycastOverlay.material.GetInstanceID()}");
    }
}