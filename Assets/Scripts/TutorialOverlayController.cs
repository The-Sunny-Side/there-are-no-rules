using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

[RequireComponent(typeof(TutorialOverlayRaycast))]
public class TutorialOverlayController : MonoBehaviour
{
    [Serializable]
    private class TutorialStep
    {
        public GameObject stepObject;
        public string textKey;
    }

    [Header("Target")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform leftStick;
    [SerializeField] private RectTransform rightStick;
    [SerializeField] private RectTransform jump;
    [SerializeField] private List<TutorialStep> steps;
    [SerializeField] private float startDelay = 0.2f;
    [SerializeField] private GameObject infoBox;

    private static readonly int PropHoleCenter    = Shader.PropertyToID("_HoleCenter");
    private static readonly int PropHoleRadius    = Shader.PropertyToID("_HoleRadius");
    private static readonly int PropHoleSoftness  = Shader.PropertyToID("_HoleSoftness");
    private static readonly int PropGlowColor     = Shader.PropertyToID("_GlowColor");
    private static readonly int PropGlowWidth     = Shader.PropertyToID("_GlowWidth");
    private static readonly int PropGlowIntensity = Shader.PropertyToID("_GlowIntensity");

    private Material _mat;
    private TutorialOverlayRaycast _raycastOverlay;
    private UiTransition infoBoxAnimator;
    private TextMeshProUGUI infoBoxText;
    private bool _shoudlShowInfoBox = false;
    private List<bool> stepsViewed = new List<bool>();
    private int currentStep = 0;

    private void Awake()
    {
        _raycastOverlay = GetComponent<TutorialOverlayRaycast>();
        _mat = _raycastOverlay.material = new Material(_raycastOverlay.material);
        infoBoxAnimator = infoBox.GetComponent<UiTransition>();
        infoBoxText = infoBox.GetComponentInChildren<TextMeshProUGUI>();
        for (int i = 0; i < steps.Count; i++)
            stepsViewed.Add(false);

        ClearHole();

        StartCoroutine(Utilities.DelayedEvent(() =>
        {
            steps[0].stepObject.SetActive(true);
        }, startDelay));
    }

    public void ResetState()
    {
        for (int i = 0; i < steps.Count; i++)
            steps[i].stepObject.SetActive(false);
    }

    public void Update()
    {
        if ((MobileInputManager.instance.leftStickRotate && currentStep == 1) ||
            (MobileInputManager.instance.jumpTapped && currentStep == 2))
        {
            Hide();
        }
    }

    public void SetHole(
        RectTransform target,
        float radiusExtra = 0f,
        float softness = 0.03f,
        Color? glowColor = null,
        float glowWidth = 0.03f,
        float glowIntensity = 1.5f)
    {
        // Usiamo la camera corretta in base al render mode del canvas
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        RectTransform overlayRT = GetComponent<RectTransform>();

        // --- Posizione normalizzata rispetto all'overlay rect ---
        // Calcoliamo il centro world del target e lo convertiamo in
        // coordinate locali dell'overlay. In questo modo siamo indipendenti
        // da Screen.width/height e da eventuali safe area o DPI scaling.
        Vector3[] targetCorners = new Vector3[4];
        target.GetWorldCorners(targetCorners);
        Vector3 worldCenter = (targetCorners[0] + targetCorners[2]) / 2f;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRT, screenPos, cam, out Vector2 localPoint);

        // overlayRT.rect: xMin/yMin sono negativi se il pivot è (0.5, 0.5)
        Rect r = overlayRT.rect;
        float normalizedX = (localPoint.x - r.xMin) / r.width;
        float normalizedY = (localPoint.y - r.yMin) / r.height;

        // --- Raggio normalizzato rispetto all'altezza dell'overlay ---
        // Misuriamo target e overlay in pixel schermo per avere unità coerenti.
        float targetHeightPx  = GetScreenSize(target).y;
        float overlayHeightPx = GetScreenSize(overlayRT).y;

        float radiusNorm = (targetHeightPx * 0.5f + radiusExtra) / overlayHeightPx;

        // --- Debug: attiva in build Android per verificare i valori ---
        Debug.Log($"[TutorialOverlay] SetHole" +
                  $"\n  screenPos={screenPos}" +
                  $"\n  localPoint={localPoint}  rect={r}" +
                  $"\n  normalized=({normalizedX:F3}, {normalizedY:F3})" +
                  $"\n  targetHeightPx={targetHeightPx:F1}  overlayHeightPx={overlayHeightPx:F1}" +
                  $"\n  radiusNorm={radiusNorm:F4}  Screen={Screen.width}x{Screen.height}");

        _mat.SetVector(PropHoleCenter,    new Vector4(normalizedX, normalizedY, 0, 0));
        _mat.SetFloat(PropHoleRadius,     radiusNorm);
        _mat.SetFloat(PropHoleSoftness,   softness);
        _mat.SetColor(PropGlowColor,      glowColor ?? new Color(0.4f, 1f, 0.95f, 1f));
        _mat.SetFloat(PropGlowWidth,      glowWidth);
        _mat.SetFloat(PropGlowIntensity,  glowIntensity);

        _raycastOverlay.SetExcluded(target, radiusExtra);
    }

    public void ClearHole()
    {
        _mat.SetFloat(PropHoleRadius, -0.3f);
        _raycastOverlay.ClearExcluded();
    }

    public void UncoverLeftStick(float delay = 0f)
    {
        leftStick.GetComponent<CanvasGroup>().interactable = true;

        StartCoroutine(Utilities.DelayedEvent(() =>
        {
            CanvasGroup cg = leftStick.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }

            SetHole(
                target: leftStick,
                radiusExtra: 12f,
                softness: 0.025f,
                glowColor: new Color(0.4f, 1f, 0.95f),
                glowWidth: 0.04f,
                glowIntensity: 2f);
        }, delay));
    }

    public void UncoverJumpButton(float delay = 0f)
    {
        StartCoroutine(Utilities.DelayedEvent(() =>
        {
            CanvasGroup cg = jump.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }

            SetHole(
                target: jump,
                radiusExtra: 12f,
                softness: 0.025f,
                glowColor: new Color(0.4f, 1f, 0.95f),
                glowWidth: 0.04f,
                glowIntensity: 2f);
        }, delay));
    }

    public void ShowStep(int index)
    {
        if (index >= 0 && index < steps.Count)
        {
            if (!stepsViewed[index])
            {
                if (steps[index].textKey != null && steps[index].textKey.Length > 0)
                {
                    LocalizedString text = new LocalizedString("ui", steps[index].textKey);
                    var handle = text.GetLocalizedStringAsync();
                    handle.Completed += (asyncOperationHandle) =>
                    {
                        infoBoxText.text = asyncOperationHandle.Result;
                        _shoudlShowInfoBox = true;
                    };
                }
                steps[currentStep].stepObject.SetActive(false);
                currentStep = index;
                Show();
                steps[index].stepObject.SetActive(true);
                stepsViewed[index] = true;
            }
        }
    }

    public void Show()
    {
        GetComponent<UiTransition>().Show();
        leftStick.GetComponent<CanvasGroup>().interactable = false;
        jump.GetComponent<CanvasGroup>().interactable = false;
        infoBoxAnimator.Hide();
    }

    public void Hide()
    {
        GetComponent<UiTransition>().Hide();
    }

    public void ShowInfoBox()
    {
        if (!_shoudlShowInfoBox)
            return;
        infoBoxAnimator.Show();
        _shoudlShowInfoBox = false;
    }

    // --- Utility ---

    private Vector2 GetScreenCenter(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) / 2f;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;
        return RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
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
