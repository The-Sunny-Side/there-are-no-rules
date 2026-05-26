using UnityEngine;
using System.Collections;
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class SlideAnimator : UiTransition
{
    [SerializeField] private float duration = 0.25f;
    [Header("Slide Positions")]
    [SerializeField] private Vector2 hiddenPosition;
    [SerializeField] private Vector2 shownPosition;
    [SerializeField] private bool noHide = true;
    [Header("Bounce Out (overshoot on arrival)")]
    [SerializeField] private bool bounceIn = false;
    [SerializeField] private bool bounceOut = false;
    [Header("Ease In (anticipation on departure)")]
    [SerializeField] private bool easeInShow = false; // <-- aggiunto
    [SerializeField] private bool easeInHide = false; // <-- aggiunto

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Coroutine anim;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        SetVisibility(initiallyVisible);
    }

    public override void Show()
    {
        if (!IsVisible)
        {
            IsVisible = true;
            StartAnim(shownPosition, true, false);
        }
    }

    public override void Hide()
    {
        if (IsVisible)
        {
            IsVisible = false;
            StartAnim(hiddenPosition, noHide, true);
        }
    }

    private void StartAnim(Vector2 targetPos, bool interactable, bool hide)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Slide(targetPos, interactable, hide));
    }

    private IEnumerator Slide(Vector2 targetPos, bool interactable, bool hide)
    {
        yield return new WaitForSeconds(hide ? onHideDelay : onShowDelay);
        canvasGroup.interactable = interactableOnAnimation;
        canvasGroup.blocksRaycasts = interactableOnAnimation;
        Vector2 startPos = rectTransform.anchoredPosition;
        float elapsed = 0f;
        bool useBounce = hide ? bounceOut : bounceIn;
        bool useEaseIn = hide ? easeInHide : easeInShow; // <-- aggiunto
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (useBounce && useEaseIn)
                t = EaseInOutBack(t);          // <-- aggiunto: entrambi
            else if (useBounce)
                t = EaseOutBack(t);
            else if (useEaseIn)
                t = EaseInBack(t);             // <-- aggiunto
            else
                t = Mathf.SmoothStep(0f, 1f, t);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, t);
            yield return null;
        }
        rectTransform.anchoredPosition = targetPos;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // <-- aggiunto
    private static float EaseInBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }

    // <-- aggiunto: bonus se li abiliti entrambi
    private static float EaseInOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c2 = c1 * 1.525f;
        return t < 0.5f
            ? Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2) / 2f
            : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (2f * t - 2f) + c2) + 2f) / 2f;
    }

    public override void HideInstant()
    {
        IsVisible = false;
        rectTransform.anchoredPosition = hiddenPosition;
        canvasGroup.interactable = noHide;
        canvasGroup.blocksRaycasts = noHide;
    }

    public override void ShowInstant()
    {
        IsVisible = true;
        rectTransform.anchoredPosition = shownPosition;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
}