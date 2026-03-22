using PurrNet;
using TMPro;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private GameObject finishedGamePanel;
    [SerializeField] private GameObject InGamePanel;
    [SerializeField] private TextMeshProUGUI textToShowWhenFinished;

    private bool _finishPanelShown;

    public void GoToHomeScreen()
    {
        pauseManager.HideUi();
        GameManager.Instance?.Resume();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayButtonAudio();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen();
        }), 0.6f));
    }

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
