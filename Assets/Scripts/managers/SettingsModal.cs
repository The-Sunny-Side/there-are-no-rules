using UnityEngine;
using UnityEngine.UI;

public class SettingsModal : MonoBehaviour
{
    public static SettingsModal Instance;

    [Header("Audio")]
    public Slider volumeSlider;
    public Toggle audioToggle;

    [Header("Animators")]
    [SerializeField] private GameObject settingsModalPanel;
    [SerializeField] private UiAnimator[] settingsModalAnimators;

    private Canvas _canvas;
    private CanvasGroup _canvasGroup;

    // =====================================================
    // LIFECYCLE
    // =====================================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            _canvas = GetComponent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Init audio state
        volumeSlider.value = AudioManager.Instance?.volume ?? 0f;
        audioToggle.SetIsOnWithoutNotify(AudioManager.Instance?.audioEnabled ?? false);

        // Listeners
        volumeSlider.onValueChanged.AddListener(UpdateVolume);
        audioToggle.onValueChanged.AddListener(OnToggleAudio);
    }

    void OnDestroy()
    {
        volumeSlider.onValueChanged.RemoveListener(UpdateVolume);
        audioToggle.onValueChanged.RemoveListener(OnToggleAudio);
    }

    // =====================================================
    // UI LOGIC
    // =====================================================

    public void Show()
    {
        OnShow();
        foreach(UiAnimator animator in settingsModalAnimators)
            animator.Show();
    }

    public void Hide()
    {
        foreach (UiAnimator animator in settingsModalAnimators)
            animator.Hide();
    }

    public void OnConfirm()
    {
        AudioManager.Instance?.PlayOneShot("notification_ok");
        Hide();
    }

    public void OnShow()
    {
        if (_canvas != null)
        { _canvas.sortingOrder = 1;
    }
    }

    public void OnHide()
    {
        if (_canvas != null)
        {
            _canvas.sortingOrder = -1;
        }
    }

    // =====================================================
    // AUDIO
    // =====================================================

    public void UpdateVolume(float volume)
    {
        AudioManager.Instance?.SetVolume(volume);
    }

    public void OnToggleAudio(bool isOn)
    {
        AudioManager.Instance?.PlayOneShot("notification_ok");
        AudioManager.Instance?.ToggleAudio();
    }
}