using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject pausePanel;

    public void Awake()
    {
        if(pausePanel != null)
        {
            pausePanel.SetActive(false );
        }
    }

    public void OnPlayButtonClick()
    {
        AudioManager.Instance.PlayOneShot("notification_ok");
        GameManager.Instance.LoadScene("ScriptingMovementeScene");
    }

    public void OnVehicleSelectionButtonClick()
    {
        AudioManager.Instance.PlayOneShot("notification_ok");
        GameManager.Instance.LoadScene("VehicleSelectionScene");
    }

    public void OnExitButtonClick()
    {
        AudioManager.Instance.PlayOneShot("notification_ok");
        GameManager.Instance.ExitGame();
    }

    public void GoToHomeScreen()
    {
        AudioManager.Instance.PlayOneShot("notification_ok");
        GameManager.Instance.GoToHomeScreen();
    }

    public void OnPauseButtonClick()
    {
        AudioManager.Instance.PlayOneShot("notification_ok");
        GameManager.Instance.Pause();
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void OnResumeButtonClick()
    {
        AudioManager.Instance.PlayOneShot("notification_ok");
        GameManager.Instance.Resume();
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void OnRestartButtonClick()
    {
        AudioManager.Instance.PlayOneShot("notification_ok");
        GameManager.Instance.LoadScene("ScriptingMovementeScene");
    }

    public void OnSettingsButtonClick()
    {
        AudioManager.Instance.PlayOneShot("notification_ok");
        GameManager.Instance.LoadScene("SettingsScene");
    }
}
