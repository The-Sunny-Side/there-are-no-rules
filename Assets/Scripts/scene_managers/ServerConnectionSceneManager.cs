using UnityEngine;

public class ServerConnectionSceneManager : MonoBehaviour
{
    [SerializeField] private UiTransition ServerConnectionButtons;

    public void OnRetryConnectionButtonClick()
    {
        ServerConnectionButtons.Hide();
        LoaderManager.Instance?.Show();
        AudioManager.Instance?.PlayButtonAudio();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.LoadSceneAsync("LocalLobbyLoadingScene");
        }), 0.3f));
    }

    public void OnMenuButtonClick()
    {
        ServerConnectionButtons.Hide();
        LoaderManager.Instance?.Show();
        AudioManager.Instance?.PlayButtonAudio();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen();
        }), 0.7f));
    }

    public void OnServerIpFound(string foundIP)
    {
        LoaderManager.Instance?.setNoHiding(false);
        GameManager.Instance?.SetIpAddress(foundIP);
        GameManager.Instance?.SetNetworkMode(Mode.Client);
        GameManager.Instance?.GoToPlayScreen();
    }

    public void OnServerIpNotFound()
    {
        LoaderManager.Instance?.setNoHiding(false);
        LoaderManager.Instance?.Hide();
        ServerConnectionButtons.Show();
    }
}
