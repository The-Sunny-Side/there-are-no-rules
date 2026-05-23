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

    private RotateAnimator noRulezLogoGearAnimator;
    private ScalePulseAnimator noRulezLogoTextAnimator;

    private List<UiAnimator> animators;
    private CanvasGroup canvasGroup;
    private LoaderType currentLoaderType = LoaderType.TheSunnySide;
    private List<UiAnimator> theSunnySideAnimators;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            theSunnySideAnimators = new List<UiAnimator>();
            canvasGroup = GetComponent<CanvasGroup>();
            theSunnySideAnimators.Add(topElement.GetComponent<UiAnimator>());
            theSunnySideAnimators.Add(bottomElement.GetComponent<UiAnimator>());
            animators = theSunnySideAnimators;
            topElement.GetComponent<Image>().color = uiConfig.loaderPrimaryColor;
            topElement.transform.Find("Text").GetComponent<Image>().color = uiConfig.loaderSecondaryColor;
            bottomElement.GetComponent<Image>().color = uiConfig.loaderSecondaryColor;
            bottomElement.transform.Find("Text").GetComponent<Image>().color = uiConfig.loaderPrimaryColor;
            noRulezLoader.GetComponent<Image>().color = uiConfig.noRulezLoaderColor;
            SceneManager.sceneLoaded += onSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            noRulezLogoGearAnimator= noRulezLoader.FindChildWithName("Logo").transform.Find("Gear").GetComponent<RotateAnimator>();
            noRulezLogoTextAnimator = noRulezLoader.FindChildWithName("Logo").transform.Find("Text").GetComponent<ScalePulseAnimator>();
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
                animators = noRulezLoader.GetComponents<UiAnimator>().ToList();
                break;
            
            case LoaderType.Garage:
                animators = garageLoader.GetComponents<UiAnimator>().ToList();
                break;
            
            default:
                animators = theSunnySideAnimators;
                break;
        }
    }

    public void Show(UnityAction callback = null)
    {
        noRulezLogoGearAnimator.animate = currentLoaderType == LoaderType.NoRulez;
        noRulezLogoTextAnimator.animate = currentLoaderType == LoaderType.NoRulez;
        UnityEvent onShowEvent = new UnityEvent();
        onShowEvent.AddListener(callback ?? new UnityAction(Utilities.DefaultCallback));
        onShowEvent.AddListener(() => {
            canvasGroup.interactable = true; 
            canvasGroup.blocksRaycasts = true;
        });
        animators[0].onShow = onShowEvent;
        foreach (UiAnimator animator in animators)
            animator?.Show();
    }

    public void Hide(UnityAction callback = null)
    {
        if (!noHiding)
        {
            noRulezLogoGearAnimator.animate = false;
            noRulezLogoTextAnimator.animate = false;
            UnityEvent onHideEvent = new UnityEvent();
            onHideEvent.AddListener(callback ?? new UnityAction(Utilities.DefaultCallback));
            onHideEvent.AddListener(() =>
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            });

            animators[0].onHide = onHideEvent;

            foreach (UiAnimator animator in animators)
                animator?.Hide();
        }
    }

    public void setNoHiding(bool nohide) { 
        noHiding = nohide;
    }
}