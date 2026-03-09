using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UiLoader : MonoBehaviour
{
    public static UiLoader Instance;

    public bool noHiding = false;
    [SerializeField] private UiConfig uiConfig;
    [SerializeField] private GameObject topElement;
    [SerializeField] private GameObject bottomElement;

    private List<UiAnimator> animators;
    private CanvasGroup canvasGroup;
    private Canvas canvas;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            animators = new List<UiAnimator>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvas = GetComponent<Canvas>();
            animators.Add(topElement.GetComponent<UiAnimator>());
            animators.Add(bottomElement.GetComponent<UiAnimator>());
            topElement.GetComponent<Image>().color = uiConfig.loaderPrimaryColor;
            topElement.transform.Find("Text").GetComponent<Image>().color = uiConfig.loaderSecondaryColor;
            bottomElement.GetComponent<Image>().color = uiConfig.loaderSecondaryColor;
            bottomElement.transform.Find("Text").GetComponent<Image>().color = uiConfig.loaderPrimaryColor;
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

    public void Show(UnityAction callback = null)
    {
        UnityEvent onShowEvent = new UnityEvent();
        onShowEvent.AddListener(callback ?? new UnityAction(Utilities.DefaultCallback));
        onShowEvent.AddListener(() => {
            canvasGroup.interactable = true; 
            canvasGroup.blocksRaycasts = true; });
        animators[0].onShow = onShowEvent;
        foreach (UiAnimator animator in animators)
            animator?.Show();
    }

    public void Hide(UnityAction callback = null)
    {
        if (!noHiding)
        {
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