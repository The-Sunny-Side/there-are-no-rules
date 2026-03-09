using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class SlideAnimator : UiAnimator
{
    [SerializeField] private float duration = 0.25f;

    [Header("Slide Positions")]
    [SerializeField] private Vector2 hiddenPosition;
    [SerializeField] private Vector2 shownPosition;
    [SerializeField] private bool noHide = true;

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

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            rectTransform.anchoredPosition =
                Vector2.LerpUnclamped(startPos, targetPos, t);

            yield return null;
        }

        rectTransform.anchoredPosition = targetPos;

        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }


    public override void HideInstant()
    {
        IsVisible=false;
        rectTransform.anchoredPosition = hiddenPosition;
        canvasGroup.interactable = noHide;
        canvasGroup.blocksRaycasts = noHide;
    }

    public override void ShowInstant()
    {
        IsVisible=true;
        rectTransform.anchoredPosition = shownPosition;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
}
