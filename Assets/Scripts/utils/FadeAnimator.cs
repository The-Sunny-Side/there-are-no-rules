using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class FadeAnimator : UiAnimator
{
    [SerializeField] private float duration = 0.25f;

    private CanvasGroup canvasGroup;
    private Coroutine anim;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        HideInstant();
    }
    public override void Show()
    {
        IsVisible=true;
        StartAnim(1f, true);
    }

    public override void Hide()
    {
        IsVisible=false;
        StartAnim(0f, false);
    }

    private void StartAnim(float targetAlpha, bool interactable)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Fade(targetAlpha, interactable));
    }

    private IEnumerator Fade(float target, bool interactable)
    {
        float start = canvasGroup.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        canvasGroup.alpha = target;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

    public override void HideInstant()
    {
        IsVisible=false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public override void ShowInstant()
    {
        IsVisible=true;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
