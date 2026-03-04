using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject finishedGamePanel;
    [SerializeField] private GameObject InGamePanel;
    [SerializeField] private GameObject[] gameButtons;
    [SerializeField] private TextMeshProUGUI textToShowWhenFinished;
    [SerializeField] private GameObject playPanel;
    [SerializeField] private UiAnimator menuButtons;

    public void Awake()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void OnPlayButtonClick()
    {
        GameManager.Instance.SetNetworkMode(Mode.Client);
        AudioManager.Instance?.PlayOneShot("notification_ok");
        string json = VehicleManager.Instance.GetVehicleJson();

        if (string.IsNullOrWhiteSpace(json))
        {
            UiLoader.Instance?.Show();
            StartCoroutine(Utilities.DelayedEvent((() =>
            {
                GameManager.Instance?.LoadSceneAsync("VehicleSelectionScene");
            })));

            return;
        }

        playPanel?.GetComponent<FadeAnimator>()?.Show();
    }

    public void OnPlayAsHost()
    {
        GameManager.Instance.SetNetworkMode(Mode.Host);
        GameManager.Instance.SetIpAddress(Utilities.GetLocalIPAddress());
        UiLoader.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.LoadSceneAsync("multiplayerMovement");
        }), 0.6f));
    }

    public void OnPlayAsClient()
    {
        menuButtons.Hide();
        playPanel?.GetComponent<FadeAnimator>()?.Hide();
        GameManager.Instance?.SetNetworkMode(Mode.Client);
        GameManager.Instance?.SetIpAddress(Utilities.GetLocalIPAddress());
        UiLoader.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        { GameManager.Instance?.LoadSceneAsync("LocalLobbyLoadingScene");
        }), 0.6f));
    }

    public void OnStartServerOnly()
    {
        playPanel?.GetComponent<FadeAnimator>()?.Hide();
        GameManager.Instance?.SetIpAddress(Utilities.GetLocalIPAddress());
        GameManager.Instance?.SetNetworkMode(Mode.ServerOnly);
        UiLoader.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.LoadSceneAsync("multiplayerMovement");
        }), 0.6f));
    }

    public void OnVehicleSelectionButtonClick()
    {
        menuButtons?.Hide();
        playPanel?.GetComponent<FadeAnimator>()?.Hide();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayOneShot("notification_ok");
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.LoadSceneAsync("VehicleSelectionScene");
        }), 0.6f));
    }

    public void OnExitButtonClick()
    {
        AudioManager.Instance?.PlayOneShot("notification_ok");
        GameManager.Instance?.ExitGame();
    }

    public void GoToHomeScreen()
    {
        GameManager.Instance?.Resume();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayOneShot("notification_ok");
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen();
        }), 0.6f));
    }

    public void OnPauseButtonClick()
    {
        AudioManager.Instance?.PlayOneShot("notification_ok");
        GameManager.Instance?.Pause();
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void OnResumeButtonClick()
    {
        AudioManager.Instance?.PlayOneShot("notification_ok");
        GameManager.Instance?.Resume();
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void OnRestartButtonClick()
    {
        GameManager.Instance?.Resume();
        AudioManager.Instance?.PlayOneShot("notification_ok");
        GameManager.Instance?.LoadScene("multiplayerMovement");
    }

    public void OnSettingsButtonClick()
    {
        AudioManager.Instance?.PlayOneShot("notification_ok");
        SettingsModal.Instance?.Show();
    }

    public void OnLoseMatch()
    {
        InGamePanel.SetActive(false);
        finishedGamePanel.SetActive(true);
        textToShowWhenFinished.text = "Hai perso, mi disp!";
    }
    public void OnWinMatch()
    {
        InGamePanel.SetActive(false);
        finishedGamePanel.SetActive(true);
        textToShowWhenFinished.text = "Congratulazione hai vinto!";
    }
}
