using PurrNet;
using TMPro;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private GameObject finishedGamePanel;
    [SerializeField] private FadeAnimator InGamePanel;
    [SerializeField] private TextMeshProUGUI textToShowWhenFinished;

    private bool _finishPanelShown;

    public void GoToHomeScreen()
    {
        pauseManager.HideUi();
        finishedGamePanel.GetComponent<FadeAnimator>()?.Hide();
        GameManager.Instance?.Resume();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayButtonAudio();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen();
        }), 0.6f));
    }

    public void OnPlayerFinished(PlayerID[] finishHumanIds, bool[] finishIsBot, int[] finishBotIds, PlayerID? localPlayerId, int totalPlayers)
    {
        if (!_finishPanelShown)
        {
            for (int i = 0; i < finishHumanIds.Length; i++)
            {
                if (finishIsBot != null && i < finishIsBot.Length && finishIsBot[i]) continue;
                if (!localPlayerId.HasValue || finishHumanIds[i] != localPlayerId.Value) continue;

                _finishPanelShown = true;
                pauseManager.HideUi();
                InGamePanel.Hide();
                finishedGamePanel.SetActive(true);
                break;
            }
        }

        if (!_finishPanelShown) return;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < finishHumanIds.Length; i++)
        {
            bool isBot = finishIsBot != null && i < finishIsBot.Length && finishIsBot[i];
            bool isMe = !isBot && localPlayerId.HasValue && finishHumanIds[i] == localPlayerId.Value;

            string label;
            if (isBot)
            {
                int botId = finishBotIds != null && i < finishBotIds.Length ? finishBotIds[i] : i + 1;
                label = $"Bot {botId}";
            }
            else if (isMe)
            {
                label = "Tu";
            }
            else
            {
                label = finishHumanIds[i].ToString();
            }

            sb.AppendLine($"{i + 1}.  {label}");
        }

        int remaining = totalPlayers - finishHumanIds.Length;
        if (remaining > 0)
            sb.AppendLine($"\n({remaining} ancora in gara...)");

        textToShowWhenFinished.text = sb.ToString();
    }
}
