using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using System.Collections;

public class InitGame : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "MainScene";

    void Start()
    {
        AudioManager.Instance?.PlayBackground("background_menu");
        StartCoroutine(Utilities.DelayedEvent((()=> {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        }), 1f));
    }
}