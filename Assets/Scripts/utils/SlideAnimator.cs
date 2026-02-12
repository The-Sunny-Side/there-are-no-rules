using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class SlideAnimator : Animator
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
        if (initiallyVisible) Show();
        else
        {
            HideInstant();
        }
    }

    public override void SetVisibility(bool visible)
    {
        IsVisible=visible;
        if (visible) Show();
        else Hide();
    }

    public override void Show()
    {
        if (!IsVisible)
        {
            IsVisible = true;
            StartAnim(shownPosition, true);
        }
    }

    public override void Hide()
    {
        if (IsVisible)
        {
            IsVisible = false;
            StartAnim(hiddenPosition, noHide);
        }
    }

    private void StartAnim(Vector2 targetPos, bool interactable)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Slide(targetPos, interactable));
    }

    private IEnumerator Slide(Vector2 targetPos, bool interactable)
    {
        // blocca input durante l’animazione
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Vector2 startPos = rectTransform.anchoredPosition;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            rectTransform.anchoredPosition =
                Vector2.Lerp(startPos, targetPos, t / duration);
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
