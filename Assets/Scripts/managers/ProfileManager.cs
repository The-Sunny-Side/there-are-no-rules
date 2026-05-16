using UnityEngine;

public class ProfileManager : MonoBehaviour
{

    [SerializeField]
    private GameObject profileModal;
    
    [SerializeField] 
    private GameObject renameModal;

    UiAnimator[] profileModalAnimators;
    UiAnimator[] playerNameAnimators;

    private void Start()
    {
        playerNameAnimators= renameModal.GetComponents<UiAnimator>();
        profileModalAnimators = profileModal.GetComponents<UiAnimator>();
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
}
