using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum LoaderType { TheSunnySide, NoRulez, Garage }

public class UiLoader : MonoBehaviour
{
    public static UiLoader Instance;

    public bool noHiding = false;

    [SerializeField] private UiConfig uiConfig;
    [SerializeField] private GameObject topElement;
    [SerializeField] private GameObject bottomElement;
    [SerializeField] private GameObject noRulezLoader;
    [SerializeField] private GameObject garageLoader;

    private List<UiAnimator> noRulezLogoAnimators;
    private List<UiTransition> animators;
    private CanvasGroup canvasGroup;
    private LoaderType currentLoaderType = LoaderType.TheSunnySide;
    private List<UiTransition> theSunnySideAnimators;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            theSunnySideAnimators = new List<UiTransition>();
            canvasGroup = GetComponent<CanvasGroup>();
            theSunnySideAnimators.Add(topElement.GetComponent<UiTransition>());
            theSunnySideAnimators.Add(bottomElement.GetComponent<UiTransition>());
            animators = theSunnySideAnimators;
            topElement.GetComponent<Image>().color = uiConfig.loaderPrimaryColor;
            topElement.transform.Find("Text").GetComponent<Image>().color = uiConfig.loaderSecondaryColor;
            bottomElement.GetComponent<Image>().color = uiConfig.loaderSecondaryColor;
            bottomElement.transform.Find("Text").GetComponent<Image>().color = uiConfig.loaderPrimaryColor;
            noRulezLoader.GetComponent<Image>().color = uiConfig.noRulezLoaderColor;
            SceneManager.sceneLoaded += onSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            noRulezLogoAnimators= new List<UiAnimator>{noRulezLoader.FindChildWithName("Logo").transform.Find("Gear").GetComponent<RotateAnimator>(), noRulezLoader.FindChildWithName("Logo").transform.Find("Text").GetComponent<ScalePulseAnimator>()};
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
        currentLoaderType = type;

        switch (type)
        {
            case LoaderType.NoRulez:
                animators = noRulezLoader.GetComponents<UiTransition>().ToList();
                break;
            
            case LoaderType.Garage:
                animators = garageLoader.GetComponents<UiTransition>().ToList();
                break;
            
            default:
                animators = theSunnySideAnimators;
                break;
        }
    }

    public void Show(UnityAction callback = null)
    {
        foreach(UiAnimator animator in noRulezLogoAnimators)
        {
            animator.animate = currentLoaderType == LoaderType.NoRulez;
        }

        UnityEvent onShowEvent = new UnityEvent();
        onShowEvent.AddListener(callback ?? new UnityAction(Utilities.DefaultCallback));
        onShowEvent.AddListener(() => {
            canvasGroup.interactable = true; 
            canvasGroup.blocksRaycasts = true;
        });
        animators[0].onShow = onShowEvent;
        foreach (UiTransition animator in animators)
            animator?.Show();
    }

    public void Hide(UnityAction callback = null)
    {
        if (!noHiding)
        {
            foreach (UiAnimator animator in noRulezLogoAnimators)
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

            animators[0].onHide = onHideEvent;

            foreach (UiTransition animator in animators)
                animator?.Hide();
        }
    }

    public void setNoHiding(bool nohide) { 
        noHiding = nohide;
    }
}