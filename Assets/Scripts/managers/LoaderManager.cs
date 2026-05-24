using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum LoaderType { TheSunnySide, NoRulez, Garage }

public class LoaderManager : MonoBehaviour
{
    public static LoaderManager Instance;

    public bool noHiding = false;

    [SerializeField] private UiConfig uiConfig;
    [SerializeField] private List<UiLoader> loaderAnimations;

    private CanvasGroup canvasGroup;
    private UiLoader currentLoader;
    private Dictionary<LoaderType, UiLoader> loadersMap;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            loadersMap = new Dictionary<LoaderType, UiLoader>();

            foreach (UiLoader loader in loaderAnimations)
            {
                loadersMap[loader.key] = loader;
            }

            canvasGroup = GetComponent<CanvasGroup>();

            currentLoader=loadersMap[LoaderType.TheSunnySide];

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

    public void switchLoader(LoaderType type) {
        currentLoader = loadersMap[type];
    }

    public void Show(UnityAction callback = null)
    {
        foreach(UiAnimator animator in currentLoader.animators)
        {
            animator.animate = true;
        }

        UnityEvent onShowEvent = new UnityEvent();
        onShowEvent.AddListener(callback ?? new UnityAction(Utilities.DefaultCallback));
        onShowEvent.AddListener(() => {
            canvasGroup.interactable = true; 
            canvasGroup.blocksRaycasts = true;
        });
        currentLoader.transitions[0].onShow = onShowEvent;
        foreach (UiTransition animator in currentLoader.transitions)
            animator?.Show();
    }

    public void Hide(UnityAction callback = null)
    {
        if (!noHiding)
        {
            foreach (UiAnimator animator in currentLoader.animators)
            {
                animator.animate = false;
            }
            UnityEvent onHideEvent = new UnityEvent();
            onHideEvent.AddListener(callback ?? new UnityAction(Utilities.DefaultCallback));
            onHideEvent.AddListener(() =>
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            });

            currentLoader.transitions[0].onHide = onHideEvent;

            foreach (UiTransition animator in currentLoader.transitions)
                animator?.Hide();
        }
    }

    public void setNoHiding(bool nohide) { 
        noHiding = nohide;
    }
}