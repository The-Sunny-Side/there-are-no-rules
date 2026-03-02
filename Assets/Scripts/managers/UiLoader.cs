using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class UiLoader : MonoBehaviour
{
    public static UiLoader Instance;

    private UiAnimator loaderAnimator;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (loaderAnimator == null)
                loaderAnimator = GetComponent<UiAnimator>();

            SceneManager.sceneLoaded += onSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name != "Init")
            Show();
    }

    private void onSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        if (scene.name != "Init")
            Hide();
    }

    public void Show(UnityAction callback =null)
    {
        UnityEvent onShowEvent = new UnityEvent();
        onShowEvent.AddListener(callback ?? new UnityAction(Utilities.DefaultCallback));
        loaderAnimator.onShow = onShowEvent;
        loaderAnimator?.Show();
    }

    public void Hide(UnityAction callback = null)
    {
        UnityEvent onHideEvent = new UnityEvent();
        onHideEvent.AddListener(callback ?? new UnityAction(Utilities.DefaultCallback));
        loaderAnimator.onHide = onHideEvent; 
        loaderAnimator?.Hide();
    }
}