using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

public class ProfileManager : MonoBehaviour
{

    [SerializeField]
    private GameObject profileModal;
    
    [SerializeField] 
    private GameObject renameModal;

    [SerializeField]
    private TextMeshProUGUI playerNameLabel;

    UiTransition[] profileModalAnimators;
    UiTransition[] playerNameAnimators;

    TextMeshProUGUI playerNameText;

    private CanvasGroup profileModalCanvasGroup;

    private void Start()
    {
        playerNameAnimators= renameModal.GetComponents<UiTransition>();
        profileModalAnimators = profileModal.GetComponents<UiTransition>();
        profileModalCanvasGroup = profileModal.GetComponent<CanvasGroup>();
        playerNameText = profileModal.transform.Find("ProfilePlayerName").Find("Text").GetComponent<TextMeshProUGUI>();
        playerNameText.text = GameConfig.Data.name;
    }
    public void ShowProfileModal()
    {
        GetComponent<UiTransition>()?.Show();
        foreach(UiTransition animator in profileModalAnimators)
        {
            animator.Show();
        }
    }

    public void HideProfileModal()
    {
        foreach (UiTransition animator in profileModalAnimators)
        {
            animator.Hide();
        }
        GetComponent<UiTransition>()?.Hide();
    }

    public void ShowRenameModal()
    {
        profileModalCanvasGroup.interactable = false;
        profileModalCanvasGroup.alpha = 0.2f;
        foreach (UiTransition animator in playerNameAnimators)
        {
            animator.Show();
        }
    }

    public void HideRenameModal()
    {
        foreach (UiTransition animator in playerNameAnimators)
        {
            animator.Hide();
        }
        profileModalCanvasGroup.interactable = true;
        profileModalCanvasGroup.alpha = 1f;
    }

    public void OnRenameConfirmClick()
    {
        string newName = renameModal.transform.Find("NameInputField").Find("TextArea").Find("Text").GetComponent<TextMeshProUGUI>().text;
        GameConfig.Data.name = newName;
        GameConfig.Save();
        playerNameText.text = newName;

        var localizeEvent = playerNameLabel.GetComponent<LocalizeStringEvent>();
        localizeEvent.StringReference.Arguments = new object[] { newName };
        localizeEvent.RefreshString();


        foreach (UiTransition animator in playerNameAnimators)
        {
            animator.Hide();
        }
        profileModalCanvasGroup.interactable = true;
        profileModalCanvasGroup.alpha = 1f;
    }
}
