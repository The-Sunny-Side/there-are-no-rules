using UnityEngine;

public class ServerConnectionSceneManager : MonoBehaviour
{
    [SerializeField] private UiTransition ServerConnectionButtons;

    public void OnRetryConnectionButtonClick()
    {
        ServerConnectionButtons.Hide();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayButtonAudio();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.LoadSceneAsync("LocalLobbyLoadingScene");
        }), 0.3f));
    }

    public void OnMenuButtonClick()
    {
        ServerConnectionButtons.Hide();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayButtonAudio();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen();
        }), 0.7f));
    }

    public void OnServerIpFound(string foundIP)
    {
        UiLoader.Instance?.setNoHiding(false);
        GameManager.Instance?.SetIpAddress(foundIP);
        GameManager.Instance?.SetNetworkMode(Mode.Client);
        GameManager.Instance?.GoToPlayScreen();
    }

    public void OnServerIpNotFound()
    {
        UiLoader.Instance?.setNoHiding(false);
        UiLoader.Instance?.Hide();
        ServerConnectionButtons.Show();
    }
}
