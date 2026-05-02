using System.Collections.Generic;
using PurrNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject countdownPanel;

    [Header("Player list")]
    [SerializeField] private Transform playerListParent;
    [SerializeField] private LobbyPlayerRow playerRowPrefab;
    [SerializeField] private TextMeshProUGUI playerCountLabel;

    [Header("Local controls")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonLabel;
    [SerializeField] private string readyText = "Pronto";
    [SerializeField] private string notReadyText = "Annulla";

    [Header("Host controls")]
    [SerializeField] private Button playButton;

    [Header("Countdown")]
    [SerializeField] private TextMeshProUGUI countdownLabel;
    [SerializeField] private string goText = "GO!";

    [Header("Refs")]
    [SerializeField] private BotSpawner botSpawner;

    private readonly Dictionary<PlayerID, LobbyPlayerRow> _rows = new();
    private LobbyState _subscribedState;

    void OnEnable()
    {
        TrySubscribe();
        if (readyButton != null) readyButton.onClick.AddListener(OnReadyClick);
        if (playButton != null) playButton.onClick.AddListener(OnPlayClick);
    }

    void OnDisable()
    {
        TryUnsubscribe();
        if (readyButton != null) readyButton.onClick.RemoveListener(OnReadyClick);
        if (playButton != null) playButton.onClick.RemoveListener(OnPlayClick);
        ClearAllRows();
    }

    private void TrySubscribe()
    {
        if (_subscribedState != null) return;
        var s = LobbyState.Instance;
        if (s == null) return;
        s.OnLobbyStateChanged += RebuildPlayerList;
        _subscribedState = s;
        RebuildPlayerList();
    }

    private void TryUnsubscribe()
    {
        if (_subscribedState != null)
        {
            _subscribedState.OnLobbyStateChanged -= RebuildPlayerList;
            _subscribedState = null;
        }
    }

    private void RebuildPlayerList()
    {
        if (LobbyState.Instance == null || playerRowPrefab == null || playerListParent == null) return;

        var current = LobbyState.Instance.Players;
        var seen = new HashSet<PlayerID>();

        foreach (var id in current)
        {
            seen.Add(id);
            if (!_rows.TryGetValue(id, out var row))
            {
                row = Instantiate(playerRowPrefab, playerListParent);
                row.Bind(id);
                _rows[id] = row;
            }
            else
            {
                row.Refresh();
            }
        }

        // rimuovi righe orfane
        var toRemove = new List<PlayerID>();
        foreach (var kv in _rows)
            if (!seen.Contains(kv.Key)) toRemove.Add(kv.Key);
        foreach (var id in toRemove)
        {
            if (_rows[id] != null) Destroy(_rows[id].gameObject);
            _rows.Remove(id);
        }
    }

    private void ClearAllRows()
    {
        foreach (var kv in _rows)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        _rows.Clear();
    }

    private void OnReadyClick()
    {
        LobbyState.Instance?.RequestToggleReady();
    }

    private void OnPlayClick()
    {
        LobbyState.Instance?.RequestStartRace();
    }

    void Update()
    {
        // Sottoscrizione lazy: LobbyState potrebbe non esistere ancora all'OnEnable
        if (_subscribedState == null) TrySubscribe();

        var state = LobbyState.Instance;
        if (state == null) return;

        if (state.IsRaceActive)
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            if (countdownPanel != null) countdownPanel.SetActive(false);
            return;
        }

        if (state.CountdownActive)
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            if (countdownPanel != null) countdownPanel.SetActive(true);
            if (countdownLabel != null)
                countdownLabel.text = state.CountdownValue > 0 ? state.CountdownValue.ToString() : goText;
            return;
        }

        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (countdownPanel != null) countdownPanel.SetActive(false);

        if (playerCountLabel != null)
        {
            int target = botSpawner != null ? botSpawner.TargetRacerCount : 8;
            playerCountLabel.text = $"{state.PlayerCount} / {target}";
        }

        if (readyButtonLabel != null)
            readyButtonLabel.text = state.IsLocalReady() ? notReadyText : readyText;

        if (readyButton != null)
            readyButton.interactable = NetworkManager.main != null && NetworkManager.main.isClient;

        if (playButton != null)
        {
            bool isHost = NetworkManager.main != null && NetworkManager.main.isHost;
            playButton.gameObject.SetActive(isHost);
            playButton.interactable = isHost && state.AreAllPlayersReady();
        }
    }
}
