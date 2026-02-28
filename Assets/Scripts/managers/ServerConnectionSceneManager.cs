using UnityEngine;

public class ServerConnectionSceneManager : MonoBehaviour
{
    [SerializeField] private UiAnimator ServerConnectionLoader;
    [SerializeField] private UiAnimator ServerConnectionButtons;

    public void OnRetryConnectionButtonClick()
    {
        AudioManager.Instance.PlayOneShot("notification_ok");
        GameManager.Instance.LoadScene("LocalLobbyLoadingScene");
    }

    public void OnMenuButtonClick()
    {
        AudioManager.Instance.PlayOneShot("notification_ok");
        GameManager.Instance.LoadScene("MainScene");
    }

    public void OnServerIpFound(string foundIP)
    {
        ServerConnectionLoader.Hide();
        GameManager.Instance.SetIpAddress(foundIP);
        GameManager.Instance.SetNetworkMode(Mode.Client);
        GameManager.Instance.LoadScene("multiplayerMovement");
    }

    public void OnServerIpNotFound()
    {
        ServerConnectionLoader.Hide();
        ServerConnectionButtons.Show();
    }
}
