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

    UiAnimator[] profileModalAnimators;
    UiAnimator[] playerNameAnimators;

    TextMeshProUGUI playerNameText;

    private void Start()
    {
        playerNameAnimators= renameModal.GetComponents<UiAnimator>();
        profileModalAnimators = profileModal.GetComponents<UiAnimator>();
        playerNameText = profileModal.transform.Find("ProfilePlayerName").Find("Text").GetComponent<TextMeshProUGUI>();
        playerNameText.text = GameConfig.Data.name;
    }
    public void ShowProfileModal()
    {
        foreach(UiAnimator animator in profileModalAnimators)
        {
            animator.Show();
        }
    }

    public void HideProfileModal()
    {
        foreach (UiAnimator animator in profileModalAnimators)
        {
            animator.Hide();
        }
    }

    public void ShowRenameModal()
    {
        foreach (UiAnimator animator in playerNameAnimators)
        {
            animator.Show();
        }
    }

    public void HideRenameModal()
    {
        foreach (UiAnimator animator in playerNameAnimators)
        {
            animator.Hide();
        }
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
        foreach (UiAnimator animator in playerNameAnimators)
        {
            animator.Hide();
        }
    }
}
