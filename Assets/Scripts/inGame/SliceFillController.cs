using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SliceFillController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("Fill")]
    [Range(0f, 1f)]
    public float FillAmount = 0f;

    [Header("Cooldown")]
    [Tooltip("Durata in secondi dell'animazione di riempimento (0 -> 1)")]
    public float CooldownDuration = 2f;
    public AnimationCurve EaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Flash on Complete")]
    [Tooltip("Se true, esegue un flash luminoso one-shot al termine dell'animazione")]
    public bool FlashOnComplete = false;
    public Color FlashColor = new Color(1f, 1f, 1f, 1f);
    [Tooltip("Durata totale del flash (andata + ritorno)")]
    public float FlashDuration = 0.35f;
    public AnimationCurve FlashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Fix")]
    [Tooltip("Inverti direzione del fill se lo shader va al contrario")]
    public bool FlipY = false;

    // ── privati ──────────────────────────────────────────────────────────
    public bool IsFilling = false;

    private Image _image;
    private Material _matInstance;
    private float _currentFill;
    private Coroutine _animCoroutine;
    private Coroutine _flashCoroutine;
    private Color _originalColor;

    private static readonly int PropFill = Shader.PropertyToID("_FillAmount");
    private static readonly int PropFillColor = Shader.PropertyToID("_FillColor");

    // ── Unity ─────────────────────────────────────────────────────────────
    void Awake()
    {
        _image = GetComponent<Image>();

        if (_image.material != null)
            _matInstance = new Material(_image.material);
        else
            Debug.LogError($"[SliceFillController] {name}: assegna un materiale con UI_FillSlice all'Image.");

        _image.material = _matInstance;
        _currentFill = FillAmount;
        _originalColor = _matInstance.GetColor(PropFillColor);
        PushToShader(_currentFill);
    }

    void OnDestroy()
    {
        if (_matInstance != null)
            Destroy(_matInstance);
    }

    // ── API pubblica ──────────────────────────────────────────────────────

    /// <summary>
    /// Anima il fill dal valore corrente verso target in CooldownDuration secondi.
    /// Passa una durata opzionale per sovrascrivere CooldownDuration al volo.
    /// </summary>
    public void FillTo(float target, float? duration = null)
    {
        target = Mathf.Clamp01(target);

        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        IsFilling = true;
        _animCoroutine = StartCoroutine(AnimateFill(target, duration ?? CooldownDuration));
    }

    /// <summary>
    /// Anima fino al massimo (1) in CooldownDuration secondi.
    /// </summary>
    public void FillToMax(float? duration = null) => FillTo(1f, duration);

    /// <summary>
    /// Resetta a 0 e anima fino a 1 in CooldownDuration secondi.
    /// Corrisponde al cooldown completo dell'arma.
    /// </summary>
    public void StartCooldown(float? duration = null)
    {
        SetImmediate(0f);
        FillToMax(duration);
    }

    /// <summary>
    /// Imposta il fill istantaneamente senza animazione.
    /// </summary>
    public void SetImmediate(float amount)
    {
        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        IsFilling = false;
        _currentFill = FillAmount = Mathf.Clamp01(amount);
        PushToShader(_currentFill);
    }

    // ── privati ──────────────────────────────────────────────────────────

    IEnumerator AnimateFill(float target, float duration)
    {
        float start = _currentFill;
        float distance = Mathf.Abs(target - start);

        if (distance < 0.001f)
        {
            IsFilling = false;
            if (FlashOnComplete) TriggerFlash();
            yield break;
        }

        // La durata passata è relativa alla distanza totale 0..1,
        // quindi se partiamo dal 50% verso 100% ci mettiamo metà del tempo.
        float actualDuration = duration * distance;
        float elapsed = 0f;

        while (elapsed < actualDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / actualDuration);
            float easedT = EaseCurve.Evaluate(t);

            _currentFill = Mathf.Lerp(start, target, easedT);
            FillAmount = _currentFill;
            PushToShader(_currentFill);

            yield return null;
        }

        _currentFill = FillAmount = target;
        PushToShader(_currentFill);
        IsFilling = false;
        _animCoroutine = null;

        if (FlashOnComplete)
            TriggerFlash();
    }

    void TriggerFlash()
    {
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        _originalColor = _matInstance.GetColor(PropFillColor);

        float half = FlashDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = FlashCurve.Evaluate(Mathf.Clamp01(elapsed / half));
            _matInstance.SetColor(PropFillColor, Color.Lerp(_originalColor, FlashColor, t));
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = FlashCurve.Evaluate(Mathf.Clamp01(elapsed / half));
            _matInstance.SetColor(PropFillColor, Color.Lerp(FlashColor, _originalColor, t));
            yield return null;
        }

        _matInstance.SetColor(PropFillColor, _originalColor);
        _flashCoroutine = null;
    }

    void PushToShader(float value)
    {
        if (_matInstance == null) return;
        float shaderValue = FlipY ? 1f - value : value;
        _matInstance.SetFloat(PropFill, shaderValue);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_matInstance == null)
        {
            var img = GetComponent<Image>();
            if (img != null && img.material != null)
                _matInstance = img.material;
        }
        if (_matInstance != null)
            PushToShader(FillAmount);
    }
#endif
}