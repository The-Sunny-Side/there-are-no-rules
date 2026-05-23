using UnityEngine;

public class ScalePulseAnimator : UiAnimator
{
    [Header("Parametri")]
    public float minScale = 0.9f;
    public float maxScale = 1.1f;
    public float speed = 2f;

    private Vector3 _originalScale;

    void Start()
    {
        _originalScale = transform.localScale;
    }

    public override void StopAnimation()
    {
        // Riporta alla scala originale quando si ferma
        transform.localScale = _originalScale;
    }

    public override void Animate()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f; // range [0, 1]
        float scaleFactor = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = _originalScale * scaleFactor;
    }
}