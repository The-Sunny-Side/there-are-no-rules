using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private UiAnimator overlay;
    [SerializeField] private UiAnimator hudPanel;
    [SerializeField] private UiAnimator pausePanel;
    [SerializeField] private UiAnimator gamePanel;

    private Canvas canvas;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
    }

    public void OnHide()
    {
        canvas.sortingOrder = 0;
    }

    public void OnShow()
    {
        canvas.sortingOrder = 1;
    }

    public void OnPauseClick()
    {
        OnShow();
        overlay.Show(); 
        pausePanel.Show();
    }

    public void OnResumeClick()
    {
        pausePanel.Hide();
        overlay.Hide();
    }

    public void HideUi()
    {
        pausePanel?.Hide();
        overlay?.Hide();
        hudPanel?.Hide();
        gamePanel?.Hide();
    }

    public void OnRestartClick()
    {
        HideUi();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayButtonAudio();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance.LoadSceneAsync(SceneManager.GetActiveScene().name);
        }), 0.6f));
        
    }

    public void OnHomeButtonClick()
    {
        HideUi();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayButtonAudio();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen();
        }), 0.6f));

    }
}
