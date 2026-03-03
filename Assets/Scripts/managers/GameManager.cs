using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool isLoading = false;

    private bool paused = false;

    private Mode mode = Mode.Host;
    private string ipAddress = "127.0.0.1";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            isLoading = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    public void LoadScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }

    public void LoadSceneAsync(string scene)
    {
        SceneManager.LoadSceneAsync(scene);
    }

    public void GoToHomeScreen()
    {
        LoadSceneAsync("MainScene");
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
        paused = true;
    }

    public void Resume()
    {
        paused = false;
    }

    public bool IsPaused()
    {
        return paused;
    }

    public void SetNetworkMode(Mode netMode) {
        mode = netMode;
    }

    public Mode GetNetworkMode() {
        return mode;
    }

    public void SetIpAddress(string newIp)
    {
        ipAddress = newIp;
    }

    public string GetIpAddress()
    {
        return ipAddress;
    }
}