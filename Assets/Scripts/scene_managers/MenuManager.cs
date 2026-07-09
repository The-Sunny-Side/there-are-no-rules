using NUnit.Framework;
using PurrNet;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private GameObject finishedGamePanel;
    [SerializeField] private FadeAnimator InGamePanel;
    [SerializeField] private GameObject leaderboardRemainingBox;
    [SerializeField] private GameObject finalPosition;
    [SerializeField] private RectTransform leaderboardContent;
    [SerializeField] private List<TextMeshProUGUI> leaderboardEntries;

    private TextMeshProUGUI leaderboardRemainingCount;
    private string[] positionSuffixesIt = new string[] { "°", "°", "°", "°", "°", "°", "°", "°" };
    private string[] positionSuffixesEn = new string[] { "st", "nd", "rd", "th", "th", "th", "th", "th" };

    private bool _finishPanelShown;

    public void Start()
    {
        PlayerHudManager.Instance?.InitPlaySceneHud();
    }


    public void GoToHomeScreen()
    {
        finalPosition.SetActive(false);
        pauseManager.HideUi();
        finishedGamePanel.GetComponent<FadeAnimator>()?.Hide();
        finishedGamePanel.GetComponentInParent<Canvas>().sortingOrder = -1;
        GameManager.Instance?.Resume();
        LoaderManager.Instance?.Show();
        AudioManager.Instance?.PlayButtonAudio();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen(true);
        }), 0.5f));
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
                string currentCode = LocalizationSettings.SelectedLocale?.Identifier.Code ?? "";
                string currentPrefix = currentCode.Substring(0, Math.Min(2, currentCode.Length));

                string[] suffixes = currentPrefix == "en" ? positionSuffixesEn : positionSuffixesIt;
                finalPosition.transform.Find("Position").GetComponent<TextMeshProUGUI>()?.SetText((i+1).ToString());
                finalPosition.transform.Find("Suffix").GetComponent<TextMeshProUGUI>()?.SetText(suffixes[i]);

                break;
            }
        }
        if (!_finishPanelShown) return;

        finalPosition.SetActive(true);

        finishedGamePanel.SetActive(true);
        leaderboardContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 65.65f * totalPlayers);
        leaderboardRemainingCount = leaderboardRemainingBox.transform.Find("RemainingNumber")?.GetComponent<TextMeshProUGUI>();

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
                label = $"{GetPlayerDisplayName(finishHumanIds[i])} (Tu)";
            }
            else
            {
                label = GetPlayerDisplayName(finishHumanIds[i]);
            }
            if(i < leaderboardEntries.Count && leaderboardEntries[i] != null)
            {
                leaderboardEntries[i].text = leaderboardEntries[i].text.Replace("---", label);
            }
            sb.AppendLine($"{i + 1}.  {label}");
        }
        int remaining = totalPlayers - finishHumanIds.Length;
        if (remaining > 0)
            leaderboardRemainingCount.text = $"{remaining}";
        else
        {
            leaderboardRemainingBox.SetActive(false);
        }
    }

    private static string GetPlayerDisplayName(PlayerID playerId)
    {
        return LobbyState.Instance != null ? LobbyState.Instance.GetDisplayName(playerId) : playerId.ToString();
    }
}