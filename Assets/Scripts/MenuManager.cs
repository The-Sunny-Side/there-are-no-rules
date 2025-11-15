using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void SelectVechicle()
    {
        AudioManager.Instance.PlayOneShot("notification_ok", 1.0f);
        SceneManager.LoadScene("VehichleSelectionScene");

    }

    public void Play()
    {
        AudioManager.Instance.PlayOneShot("notification_ok", 1.0f);
        SceneManager.LoadScene("PlayScene");

    }

    public void GoHome()
    {
        AudioManager.Instance.PlayOneShot("notification_ok", 1.0f);
        SceneManager.LoadScene("MainScene");

    }

    public void Esci()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}