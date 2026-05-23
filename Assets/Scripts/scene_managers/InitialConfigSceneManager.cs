using TMPro;
using UnityEngine;

public class InitialConfigSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject nameModal;
    [SerializeField] private GameObject tutorialModal;

    private TextMeshProUGUI nameInputText;
    private UiTransition[] nameModalAnimators = { };
    private UiTransition[] tutorialModalAnimators = { };

    void Start()
    {
        nameModalAnimators = nameModal.GetComponents<UiTransition>();

        tutorialModalAnimators = tutorialModal.GetComponents<UiTransition>();

        nameInputText = nameModal.transform.Find("NameInputField").Find("TextArea").Find("Text").GetComponent<TextMeshProUGUI>();
    }

    public void onShowTutorialButtonClick()
    {
        AudioManager.Instance?.PlayButtonAudio();

        foreach (UiTransition animator in nameModalAnimators)
        {
            animator.Hide();
        }
        foreach (UiTransition animator in tutorialModalAnimators)
        {
            animator.Hide();
        }

        LoaderManager.Instance?.switchLoader(LoaderType.NoRulez);

        LoaderManager.Instance?.Show();
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
        foreach (UiTransition animator in nameModalAnimators)
        {
            animator.Hide();
        }

        foreach (UiTransition animator in tutorialModalAnimators)
        {
            animator.Show();
        }


    }
    public void onTutorialSkipButtonClick()
    {
        AudioManager.Instance?.PlayButtonAudio();

        foreach (UiTransition animator in tutorialModalAnimators)
        {
            animator.Hide();
        }
        LoaderManager.Instance?.switchLoader(LoaderType.NoRulez);
        LoaderManager.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen();
        }), 0.6f));
    }
}
