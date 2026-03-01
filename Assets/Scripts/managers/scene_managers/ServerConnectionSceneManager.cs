using UnityEngine;

public class ServerConnectionSceneManager : MonoBehaviour
{
    [SerializeField] private UiAnimator ServerConnectionLoader;
    [SerializeField] private UiAnimator ServerConnectionButtons;

    public void OnRetryConnectionButtonClick()
    {
        ServerConnectionButtons.Hide();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayOneShot("notification_ok");
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.LoadSceneAsync("LocalLobbyLoadingScene");
        }), 0.3f));
    }

    public void OnMenuButtonClick()
    {
        ServerConnectionButtons.Hide();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayOneShot("notification_ok");
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen();
        }), 0.7f));
    }

    public void OnServerIpFound(string foundIP)
    {
        ServerConnectionLoader.Hide();
        GameManager.Instance?.SetIpAddress(foundIP);
        GameManager.Instance?.SetNetworkMode(Mode.Client);
        GameManager.Instance?.LoadScene("multiplayerMovement");
    }

    public void OnServerIpNotFound()
    {
        ServerConnectionLoader.Hide();
        ServerConnectionButtons.Show();
    }
}
