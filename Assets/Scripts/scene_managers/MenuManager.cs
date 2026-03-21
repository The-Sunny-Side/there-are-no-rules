using PurrNet;
using TMPro;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject finishedGamePanel;
    [SerializeField] private GameObject InGamePanel;
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
        AudioManager.Instance?.PlayButtonAudio();
        string json = VehicleManager.Instance.GetVehicleJson();

        if (string.IsNullOrWhiteSpace(json))
        {
            UiLoader.Instance?.Show();
            StartCoroutine(Utilities.DelayedEvent((() =>
            {
                GameManager.Instance?.GoToVehicleScreen();
            })));

            return;
        }

        playPanel?.GetComponent<FadeAnimator>()?.Show();
    }

    public void OnPlayAsHost()
    {
        menuButtons.Hide();
        playPanel?.GetComponent<FadeAnimator>()?.Hide();
        GameManager.Instance.SetNetworkMode(Mode.Host);
        GameManager.Instance.SetIpAddress(Utilities.GetLocalIPAddress());
        UiLoader.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToPlayScreen();
        }), 0.6f));
    }

    public void OnPlayAsClient()
    {
        menuButtons.Hide();
        playPanel?.GetComponent<FadeAnimator>()?.Hide();
        GameManager.Instance?.SetNetworkMode(Mode.Client);
        GameManager.Instance?.SetIpAddress(Utilities.GetLocalIPAddress());
        UiLoader.Instance?.Show();
        UiLoader.Instance.setNoHiding(true);
        StartCoroutine(Utilities.DelayedEvent((() =>
        { GameManager.Instance?.GoToLocalLobbyScreen();
        }), 0.6f));
    }

    public void OnStartServerOnly()
    {
        menuButtons.Hide();
        playPanel?.GetComponent<FadeAnimator>()?.Hide();
        GameManager.Instance?.SetIpAddress(Utilities.GetLocalIPAddress());
        GameManager.Instance?.SetNetworkMode(Mode.ServerOnly);
        UiLoader.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToPlayScreen();
        }), 0.6f));
    }

    public void OnVehicleSelectionButtonClick()
    {
        menuButtons?.Hide();
        playPanel?.GetComponent<FadeAnimator>()?.Hide();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayButtonAudio();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToVehicleScreen();
        }), 0.6f));
    }

    public void OnExitButtonClick()
    {
        AudioManager.Instance?.PlayButtonAudio();
        GameManager.Instance?.ExitGame();
    }

    public void GoToHomeScreen()
    {
        GameManager.Instance?.Resume();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayButtonAudio();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen();
        }), 0.6f));
    }

    public void OnPauseButtonClick()
    {
        AudioManager.Instance?.PlayButtonAudio();
        GameManager.Instance?.Pause();
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void OnResumeButtonClick()
    {
        AudioManager.Instance?.PlayButtonAudio();
        GameManager.Instance?.Resume();
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void OnRestartButtonClick()
    {
        GameManager.Instance?.Resume();
        AudioManager.Instance?.PlayButtonAudio();
        GameManager.Instance?.GoToPlayScreen();
    }

    public void OnSettingsButtonClick()
    {
        AudioManager.Instance?.PlayButtonAudio();
        SettingsModal.Instance?.Show();
    }

    private bool _finishPanelShown;

    public void OnPlayerFinished(PlayerID[] finishOrder, PlayerID? localPlayerId, int totalPlayers)
    {
        // mostra il pannello solo quando arriva il giocatore locale
        if (!_finishPanelShown)
        {
            foreach (var id in finishOrder)
            {
                if (localPlayerId.HasValue && id == localPlayerId.Value)
                {
                    _finishPanelShown = true;
                    InGamePanel.SetActive(false);
                    finishedGamePanel.SetActive(true);
                    break;
                }
            }
        }

        if (!_finishPanelShown) return;

        // aggiorna la classifica
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < finishOrder.Length; i++)
        {
            bool isMe = localPlayerId.HasValue && finishOrder[i] == localPlayerId.Value;
            string label = isMe ? "Tu" : $"Avversario {i + 1}";
            sb.AppendLine($"{i + 1}°  {label}");
        }

        int remaining = totalPlayers - finishOrder.Length;
        if (remaining > 0)
            sb.AppendLine($"\n({remaining} ancora in gara...)");

        textToShowWhenFinished.text = sb.ToString();
    }
}
