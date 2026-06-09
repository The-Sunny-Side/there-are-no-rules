using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialSceneManager : MonoBehaviour
{
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private TutorialOverlayController tutorialUiController;

    public void HideUi()
    {
        pauseManager.HideUi();
        tutorialUiController.Hide();
    }
public void OnMenuButtonClick()
    {
        HideUi();
        AudioManager.Instance?.PlayButtonAudio();
        LoaderManager.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen(true);
        }), 0.5f));
    }

    public void OnRestartButtonClick()
    {
        HideUi();
        AudioManager.Instance?.PlayButtonAudio();
        LoaderManager.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance.LoadSceneAsync(SceneManager.GetActiveScene().name);
        }), 0.6f));
    }
}
