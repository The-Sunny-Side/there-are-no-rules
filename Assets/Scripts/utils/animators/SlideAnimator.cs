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
    [SerializeField] private bool easeInShow = false;
    [SerializeField] private bool easeInHide = false;

    [Header("Depart Effects")]
    [SerializeField] private bool bounceOnDepart = false;
    [SerializeField][Range(0f, 0.3f)] private float bounceOnDepartAmount = 0.08f;
    [SerializeField] private bool tiltOnDepart = false;
    [SerializeField][Range(0f, 20f)] private float tiltMaxAngle = 8f;

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
        bool useBounce = hide ? bounceOut : bounceIn;
        bool useEaseIn = hide ? easeInHide : easeInShow;

        // --- Bounce on depart: offset startPos nella direzione opposta ---
        Vector2 actualStartPos = startPos;
        if (bounceOnDepart)
        {
            Vector2 dir = (targetPos - startPos).normalized;
            actualStartPos = startPos - dir * (targetPos - startPos).magnitude * bounceOnDepartAmount;
        }
        rectTransform.anchoredPosition = actualStartPos;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Curva principale (invariata)
            if (useBounce && useEaseIn)
                t = EaseInOutBack(t);
            else if (useBounce)
                t = EaseOutBack(t);
            else if (useEaseIn)
                t = EaseInBack(t);
            else
                t = Mathf.SmoothStep(0f, 1f, t);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(actualStartPos, targetPos, t);

            // --- Tilt: inclinazione sull'asse Z in base alla direzione del movimento ---
            if (tiltOnDepart)
            {
                Vector2 dir = targetPos - startPos;
                // Il tilt è massimo a t=0, si annulla verso t=1
                // Segno: movimento verso destra → inclinazione positiva (orario), sinistra → negativa
                // Per movimento verticale usiamo la X della direzione perpendicolare
                float tiltSign = Mathf.Sign(dir.x != 0f ? dir.x : -dir.y);
                float tiltCurve = Mathf.Pow(1f - t, 2f); // smooth decay
                float angle = tiltSign * tiltMaxAngle * tiltCurve;
                rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
            }

            yield return null;
        }

        rectTransform.anchoredPosition = targetPos;

        // Assicura rotazione pulita a fine animazione
        if (tiltOnDepart)
            rectTransform.localEulerAngles = Vector3.zero;

        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

    // --- Easing functions (invariate) ---

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static float EaseInBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }

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
        rectTransform.localEulerAngles = Vector3.zero;
        canvasGroup.interactable = noHide;
        canvasGroup.blocksRaycasts = noHide;
    }

    public override void ShowInstant()
    {
        IsVisible = true;
        rectTransform.anchoredPosition = shownPosition;
        rectTransform.localEulerAngles = Vector3.zero;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
}