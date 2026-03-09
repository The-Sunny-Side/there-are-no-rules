using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private UiAnimator overlay;
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

    public void OnRestartClick()
    {
        pausePanel?.Hide();
        overlay?.Hide();
        gamePanel?.Hide();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayOneShot("notification_ok");
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance.LoadSceneAsync(SceneManager.GetActiveScene().name);
        }), 0.6f));
        
    }

    public void OnHomeButtonClick()
    {
        pausePanel?.Hide();
        overlay?.Hide();
        gamePanel?.Hide();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayOneShot("notification_ok");
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen();
        }), 0.6f));

    }
}
