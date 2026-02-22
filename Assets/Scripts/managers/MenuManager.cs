using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject finishedGamePanel;
    [SerializeField] private GameObject InGamePanel;
    [SerializeField] private GameObject[] gameButtons;
    [SerializeField] private TextMeshProUGUI textToShowWhenFinished;
    [SerializeField] private GameObject playPanel;
    private TMP_InputField IpField;
    public void Awake()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (playPanel != null)
        {
            IpField = playPanel.transform.Find("IpField").GetComponent<TMP_InputField>();
        }
    }

    public void OnPlayButtonClick()
    {
        GameManager.Instance.SetNetworkMode(Mode.Host);
        string json = VehicleManager.Instance.GetVehicleJson();

        if (string.IsNullOrWhiteSpace(json))
        {
            GameManager.Instance.LoadScene("VehicleSelectionScene");
            AudioManager.Instance.PlayOneShot("notification_ok");
            return;
        }

        playPanel.GetComponent<FadeAnimator>().Show();
        AudioManager.Instance.PlayOneShot("notification_ok");

        if (IpField != null)
        {
            IpField.text = Utilities.GetLocalIPAddress();
            GameManager.Instance.SetIpAddress(IpField.text);
        }
    }

    public void OnPlayConfirm()
    {
        GameManager.Instance.LoadScene("multiplayerMovement");
    }

    public void OnStartServerOnly()
    {
        GameManager.Instance.SetIpAddress(Utilities.GetLocalIPAddress());
        GameManager.Instance.SetNetworkMode(Mode.ServerOnly);
        GameManager.Instance.LoadScene("multiplayerMovement");
    }

    public void OnNetworkModeChange(bool isHost)
    {
        if (IpField != null)
        {
            IpField.readOnly = isHost;
            if (isHost)
            {
                IpField.text = Utilities.GetLocalIPAddress();
            }

            Image inputBg = IpField.GetComponent<Image>();
            if (inputBg != null)
            {
                inputBg.color = isHost ? new Color(255, 255, 255, 0) : Color.white;
            }


        }
        GameManager.Instance.SetNetworkMode(isHost ? Mode.Host : Mode.Client);
    }

    public void OnSelectedIpChange(string selectedIp)
    {
        GameManager.Instance.SetIpAddress(selectedIp);
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
        GameManager.Instance.LoadScene("multiplayerMovement");
    }

    public void OnSettingsButtonClick()
    {
        AudioManager.Instance.PlayOneShot("notification_ok");
        GameManager.Instance.LoadScene("SettingsScene");
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
