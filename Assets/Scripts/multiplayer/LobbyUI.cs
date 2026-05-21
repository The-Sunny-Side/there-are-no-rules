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
    [SerializeField] private bool instantiateMissingRows = true;

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

    private readonly List<LobbyPlayerRow> _rowPool = new();
    private LobbyState _subscribedState;

    void OnEnable()
    {
        RebuildRowPool();
        HideAllRows();
        TrySubscribe();
        if (readyButton != null) readyButton.onClick.AddListener(OnReadyClick);
        if (playButton != null) playButton.onClick.AddListener(OnPlayClick);
    }

    void OnDisable()
    {
        TryUnsubscribe();
        if (readyButton != null) readyButton.onClick.RemoveListener(OnReadyClick);
        if (playButton != null) playButton.onClick.RemoveListener(OnPlayClick);
        HideAllRows();
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
        if (LobbyState.Instance == null || playerListParent == null) return;

        var current = LobbyState.Instance.Players;
        EnsureRowCapacity(current.Count);

        int index = 0;
        foreach (var id in current)
        {
            if (index >= _rowPool.Count) break;

            var row = _rowPool[index++];
            if (row == null) continue;

            if (!row.gameObject.activeSelf) row.gameObject.SetActive(true);
            row.Bind(id);
        }

        for (int i = index; i < _rowPool.Count; i++)
        {
            var row = _rowPool[i];
            if (row == null) continue;

            row.Clear();
            if (row.gameObject.activeSelf) row.gameObject.SetActive(false);
        }
    }

    private void HideAllRows()
    {
        foreach (var row in _rowPool)
        {
            if (row == null) continue;

            row.Clear();
            if (row.gameObject.activeSelf) row.gameObject.SetActive(false);
        }
    }

    private void RebuildRowPool()
    {
        _rowPool.Clear();
        if (playerListParent == null) return;

        for (int i = 0; i < playerListParent.childCount; i++)
        {
            var child = playerListParent.GetChild(i);
            if (child.TryGetComponent<LobbyPlayerRow>(out var row))
                _rowPool.Add(row);
        }
    }

    private void EnsureRowCapacity(int requiredCount)
    {
        RebuildRowPool();
        if (!instantiateMissingRows || playerRowPrefab == null || playerListParent == null) return;

        while (_rowPool.Count < requiredCount)
        {
            var row = Instantiate(playerRowPrefab, playerListParent, false);
            row.gameObject.SetActive(false);
            _rowPool.Add(row);
        }
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

        if (lobbyPanel != null) lobbyPanel.SetActive(LobbyState.Instance.showLobbyPanel);
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
