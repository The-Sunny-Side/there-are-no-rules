using UnityEngine;
using System.Collections;

public class TiltTransition : UiTransition
{
    [SerializeField] private float duration = 0.25f;
    [Header("Show Tilt")]
    [SerializeField] private float showTiltAngle = 15f;
    [SerializeField] private Vector3 showTiltAxis = Vector3.forward;
    [Header("Hide Tilt")]
    [SerializeField] private float hideTiltAngle = 15f;
    [SerializeField] private Vector3 hideTiltAxis = Vector3.forward;

    private Quaternion initialRotation;
    private Coroutine anim;
    private Transform cachedTransform;

    void Awake()
    {
        cachedTransform = transform;
        initialRotation = cachedTransform.localRotation;
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
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(TiltAndReturn(hide));
    }

    private IEnumerator TiltAndReturn(bool hide)
    {
        yield return new WaitForSeconds(hide ? onHideDelay : onShowDelay);

        float angle = hide ? hideTiltAngle : showTiltAngle;
        Vector3 axis = hide ? hideTiltAxis : showTiltAxis;
        Quaternion tiltTarget = initialRotation * Quaternion.AngleAxis(angle, axis);

        float halfDuration = duration * 0.5f;

        // Fase 1: vai al tilt
        float elapsed = 0f;
        Quaternion start = cachedTransform.localRotation;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutBack(Mathf.Clamp01(elapsed / halfDuration));
            cachedTransform.localRotation = Quaternion.LerpUnclamped(start, tiltTarget, t);
            yield return null;
        }
        cachedTransform.localRotation = tiltTarget;

        // Fase 2: torna alla rotazione originale
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / halfDuration));
            cachedTransform.localRotation = Quaternion.LerpUnclamped(tiltTarget, initialRotation, t);
            yield return null;
        }
        cachedTransform.localRotation = initialRotation;

        (hide ? onHide : onShow)?.Invoke();
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    public override void ShowInstant()
    {
        IsVisible = true;
        if (cachedTransform != null)
            cachedTransform.localRotation = initialRotation;
    }

    public override void HideInstant()
    {
        IsVisible = false;
        if (cachedTransform != null)
            cachedTransform.localRotation = initialRotation;
    }
}