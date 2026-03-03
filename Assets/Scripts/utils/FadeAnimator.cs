using UnityEngine;
using System.Collections;
using UnityEngine.Events;

[RequireComponent(typeof(CanvasGroup))]
public class FadeAnimator : UiAnimator
{
    [SerializeField] private float duration = 0.25f;

    private float maxAlpha = 1f;
    private float minAlpha = 0f;

    private CanvasGroup canvasGroup;
    private Coroutine anim;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (!initiallyVisible) HideInstant();
    }
    public override void Show()
    {
        IsVisible = true;
        StartAnim(false, interactableOnAnimation);
    }

    public override void Hide()
    {
        IsVisible = false;
        StartAnim(true, interactableOnAnimation);
    }

    private void StartAnim(bool hide, bool interactable)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Fade(hide, interactableOnAnimation, hide ? onHide : onShow));
    }

    private IEnumerator Fade(bool hide, bool interactable, UnityEvent onEnd)
    {
        yield return new WaitForSeconds(hide?onHideDelay:onShowDelay);
        canvasGroup.interactable = interactableOnAnimation;

        float start = canvasGroup.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, hide ? minAlpha : maxAlpha, t / duration);
            yield return null;
        }

        canvasGroup.alpha = hide ? minAlpha : maxAlpha;
        canvasGroup.interactable = !hide;
        canvasGroup.blocksRaycasts = !hide;
        onEnd?.Invoke();
    }

    public override void HideInstant()
    {
        IsVisible = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public override void ShowInstant()
    {
        IsVisible = true;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
