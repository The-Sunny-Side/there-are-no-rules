using UnityEngine;
using UnityEngine.SceneManagement;

public class InitGame : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "MainScene";
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private GameConfig gameConfig;

    private void Awake()
    {
        if (audioManager == null && AudioManager.Instance)
        {
            audioManager=AudioManager.Instance;
        }
    }

    void Start()
    {
        audioManager?.PlayBackground("background_menu");

        if (gameConfig.GetConfigData().highFpsMode)
        {
          Application.targetFrameRate = 60;
        }
        else { 
          Application.targetFrameRate = -1;
        }

        if (string.IsNullOrEmpty(gameConfig.GetConfigData().name))
        {
            StartCoroutine(Utilities.DelayedEvent((() =>
            {
                audioManager?.PlayBackground("background_menu");
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("InitialConfigScene");
            }), 1f));
        }
        else
        {
            StartCoroutine(Utilities.DelayedEvent((() =>
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
            }), 1f));
        }
    }
}