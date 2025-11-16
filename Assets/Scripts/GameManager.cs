using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private bool paused = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string scene)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    public void GoToHomeScreen()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene");
    }

    public void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }



    public void Pause()
    {
        Time.timeScale = 0f;
        paused = true;
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        paused = false;
    }

    public bool IsPaused()
    {
        return paused;
    }
}