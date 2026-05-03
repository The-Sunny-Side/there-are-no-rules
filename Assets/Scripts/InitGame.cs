using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using System.Collections;

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