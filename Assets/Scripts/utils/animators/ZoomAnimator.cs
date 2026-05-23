using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class ZoomAnimator : UiTransition
{
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private Vector3 shownScale = Vector3.one;
    [SerializeField] private Vector3 hiddenScale = Vector3.zero;

    private Coroutine anim;
    private Transform cachedTransform;

    void Awake()
    {
        cachedTransform = transform;

        if (!initiallyVisible)
            HideInstant();
        else
            ShowInstant();
    }

    public override void Show()
    {
        IsVisible = true;
        StartAnim(false);
    }

    public override void Hide()
    {
        IsVisible = false;
        StartAnim(true);
    }

    private void StartAnim(bool hide)
    {
        if (anim != null)
            StopCoroutine(anim);

        anim = StartCoroutine(Zoom(
            hide,
            hide ? onHide : onShow
        ));
    }

    private IEnumerator Zoom(bool hide, UnityEvent onEnd)
    {
        yield return new WaitForSeconds(hide ? onHideDelay : onShowDelay);

        Vector3 start = cachedTransform.localScale;
        Vector3 target = hide ? hiddenScale : shownScale;

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = t / duration;

            cachedTransform.localScale = Vector3.Lerp(start, target, lerp);
            yield return null;
        }

        cachedTransform.localScale = target;
        onEnd?.Invoke();
    }

    public override void HideInstant()
    {
        IsVisible = false;
        if (cachedTransform != null)
            cachedTransform.localScale = hiddenScale;
    }

    public override void ShowInstant()
    {
        IsVisible = true;
        if (cachedTransform != null)
            cachedTransform.localScale = shownScale;
    }
}