using PurrNet;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

// Riga UI per un singolo player nella lobby. Mostra nome + indicatore ready.
// Va istanziata dal LobbyUI come prefab e bindata a un PlayerID.
public class LobbyPlayerRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI readyLabel;
    [SerializeField] private string notReadyText = "...";
    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color notReadyColor = Color.gray;

    private PlayerID _id;
    private bool _bound;

    private string readyText = "READY";

    public void Awake()
    {
        // Traduzione dei testi
        if (LocalizationSettings.Instance != null)
        {
            readyText = LocalizationSettings.StringDatabase.GetLocalizedString("ui", "ready");
        }
    }


    public void Bind(PlayerID id)
    {
        _id = id;
        _bound = true;
        Debug.Log("nome:"+LobbyState.Instance.GetDisplayName(id));
        if (nameLabel != null)
            nameLabel.text = LobbyState.Instance != null ? LobbyState.Instance.GetDisplayName(id) : id.ToString();
        Refresh();
    }

    public void Refresh()
    {
        if (!_bound || readyLabel == null) return;
        bool ready = LobbyState.Instance != null && LobbyState.Instance.IsReady(_id);
        readyLabel.text = ready ? readyText : notReadyText;
        readyLabel.color = ready ? readyColor : notReadyColor;
    }

    public void Clear()
    {
        _id = default;
        _bound = false;

        if (nameLabel != null) nameLabel.text = string.Empty;
        if (readyLabel != null)
        {
            readyLabel.text = string.Empty;
            readyLabel.color = notReadyColor;
        }
    }

    public PlayerID Id => _id;
}
