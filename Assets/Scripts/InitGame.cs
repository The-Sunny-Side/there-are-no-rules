using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using System.Collections;

public class InitGame : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "MainScene";
    [SerializeField] private AudioManager audioManager;

    private void Awake()
    {
        if (audioManager == null && AudioManager.Instance)
        {
            audioManager=AudioManager.Instance;
        }
    }

    void Start()
    {
        
        StartCoroutine(Utilities.DelayedEvent((()=> {
            audioManager?.PlayBackground("background_menu");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        }), 1f));
    }
}