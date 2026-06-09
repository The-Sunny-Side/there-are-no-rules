using UnityEngine;

public class TiltAnimator : UiAnimator
{
    [Header("Scale Bounce")]
    [SerializeField] private float scaleAmplitude = 0.08f;
    [SerializeField] private float scaleSpeed = 2.2f;

    [Header("Tilt")]
    [SerializeField] private float tiltAmplitude = 8f;
    [SerializeField] private float tiltSpeed = 1.6f;

    private RectTransform _rt;
    private Vector3 _baseScale;
    private float _time;
    private float _scalePhase;
    private float _tiltPhase;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _baseScale = _rt.localScale;
        _scalePhase = Random.Range(0f, Mathf.PI * 2f);
        _tiltPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    public override void Animate()
    {
        _time += Time.deltaTime;

        float s = 1f + Mathf.Sin(_time * scaleSpeed + _scalePhase) * scaleAmplitude;
        float angle = Mathf.Sin(_time * tiltSpeed + _tiltPhase) * tiltAmplitude;

        _rt.localScale = new Vector3(_baseScale.x * s, _baseScale.y * s, _baseScale.z);
        _rt.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    public override void StopAnimation()
    {
        _rt.localScale = _baseScale;
        _rt.localEulerAngles = Vector3.zero;
    }
}