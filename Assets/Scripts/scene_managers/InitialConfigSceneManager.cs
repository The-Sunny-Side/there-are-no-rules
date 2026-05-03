using TMPro;
using UnityEngine;

public class InitialConfigSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject nameModal;
    [SerializeField] private GameObject tutorialModal;

    private TextMeshProUGUI nameInputText;
    private UiAnimator[] nameModalAnimators = { };
    private UiAnimator[] tutorialModalAnimators = { };

    void Start()
    {
        nameModalAnimators = nameModal.GetComponents<UiAnimator>();

        tutorialModalAnimators = tutorialModal.GetComponents<UiAnimator>();

        nameInputText = nameModal.transform.Find("NameInputField").Find("TextArea").Find("Text").GetComponent<TextMeshProUGUI>();
    }

    public void onShowTutorialButtonClick()
    {
        AudioManager.Instance?.PlayButtonAudio();

        foreach (UiAnimator animator in nameModalAnimators)
        {
            animator.Hide();
        }
        foreach (UiAnimator animator in tutorialModalAnimators)
        {
            animator.Hide();
        }
        UiLoader.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToSandboxScreen();
        }), 0.6f));
    }

    public void onNameConfirmButtonClick()
    {

        string newName = nameInputText.text;

        if (string.IsNullOrEmpty(newName))
        {
            return;
        }

        AudioManager.Instance?.PlayButtonAudio();

        GameConfig.Data.name = newName;
        GameConfig.Save();
        foreach (UiAnimator animator in nameModalAnimators)
        {
            animator.Hide();
        }

        foreach (UiAnimator animator in tutorialModalAnimators)
        {
            animator.Show();
        }


    }
    public void onTutorialSkipButtonClick()
    {
        AudioManager.Instance?.PlayButtonAudio();

        foreach (UiAnimator animator in tutorialModalAnimators)
        {
            animator.Hide();
        }
        UiLoader.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen();
        }), 0.6f));
    }
}
