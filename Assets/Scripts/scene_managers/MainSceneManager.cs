using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class MainSceneManager : MonoBehaviour
{
    [SerializeField] private UiAnimator menu;
    [SerializeField] private GameObject playPanel;
    [SerializeField] private UiAnimator menuButtons;
    [SerializeField] private UiAnimator playerName;
    [SerializeField] private Composer composer;
    [SerializeField] private GameObject nameModal;
    [SerializeField] private GameObject logo;

    private UiAnimator[] nameModalAnimators;
    private TMP_InputField nameInputField;
    private UiAnimator[] logoAnimators;

    public void Awake()
    {
        nameModalAnimators = nameModal.GetComponents<UiAnimator>();
        nameInputField = nameModal.transform.Find("NameInputField").GetComponent<TMP_InputField>();
        logoAnimators = logo.GetComponents<UiAnimator>();
        nameInputField.text = GameConfig.Data.name;

        Dictionary<string, VehicleEntry> list = VehicleManager.Instance?.LoadVehicleConfig();


        if (list.TryGetValue(VehicleElementsKeys.Body, out VehicleEntry bodyElement) && (bodyElement?.element != null) &&
             list.TryGetValue(VehicleElementsKeys.Base, out VehicleEntry baseElement) && baseElement?.element != null)
        {
            composer.baseElement = Instantiate(baseElement.element);
            composer.bodyElement = Instantiate(bodyElement.element);

            if (list.TryGetValue(VehicleElementsKeys.WeaponBack, out VehicleEntry backWeaponElement) && backWeaponElement?.element != null)
                composer.weaponBackElement = Instantiate(backWeaponElement.element);

            if (list.TryGetValue(VehicleElementsKeys.WeaponFront, out VehicleEntry frontWeaponElement) && frontWeaponElement?.element != null)
                composer.weaponFrontElement = Instantiate(frontWeaponElement.element);

            if (list.TryGetValue(VehicleElementsKeys.WeaponLeft, out VehicleEntry leftWeaponElement) && leftWeaponElement?.element != null)
                composer.weaponLeftElement = Instantiate(leftWeaponElement.element);

            if (list.TryGetValue(VehicleElementsKeys.WeaponRight, out VehicleEntry rightWeaponElement) && rightWeaponElement?.element != null)
                composer.weaponRightElement = Instantiate(rightWeaponElement.element);

            composer.AlignComponents();
        }
    }
    public void Start()
    {
        InitMenu();
        UiLoader.Instance?.switchLoader(LoaderType.NoRulez);
    }

    public void HideHud()
    {
        foreach(UiAnimator animator in logoAnimators)
        {
            animator.Hide();
        }
        playerName.Hide();
        menuButtons.Hide();
        playPanel?.GetComponent<UiAnimator>()?.Hide();
    }
    public void OnPlayButtonClick()
    {
        AudioManager.Instance?.PlayButtonAudio();
        string json = VehicleManager.Instance?.GetVehicleJson();

        if (string.IsNullOrWhiteSpace(json))
        {
            HideHud();
            UiLoader.Instance?.Show();
            StartCoroutine(Utilities.DelayedEvent((() =>
            {
                GameManager.Instance?.GoToVehicleScreen();
            })));

            return;
        }

        playPanel?.GetComponent<UiAnimator>()?.ToggleAnimator();
    }

    public void OnPlayAsHost()
    {
        HideHud();
        GameManager.Instance.SetNetworkMode(Mode.Host);
        GameManager.Instance.SetIpAddress(Utilities.GetLocalIPAddress());
        UiLoader.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToPlayScreen();
        }), 0.6f));
    }

    public void OnPlayAsClient()
    {
        HideHud();
        GameManager.Instance?.SetNetworkMode(Mode.Client);
        GameManager.Instance?.SetIpAddress(Utilities.GetLocalIPAddress());
        UiLoader.Instance?.Show();
        UiLoader.Instance.setNoHiding(true);
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToLocalLobbyScreen();
        }), 0.6f));
    }

    public void OnStartServerOnly()
    {
        HideHud();
        GameManager.Instance?.SetIpAddress(Utilities.GetLocalIPAddress());
        GameManager.Instance?.SetNetworkMode(Mode.ServerOnly);
        UiLoader.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToPlayScreen();
        }), 0.6f));
    }

    public void OnVehicleSelectionButtonClick()
    {
        HideHud();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayButtonAudio();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToVehicleScreen();
        }), 0.6f));
    }

    public void OnExitButtonClick()
    {
        AudioManager.Instance?.PlayButtonAudio();
        GameManager.Instance?.ExitGame();
    }

    public void OnSettingsButtonClick()
    {
        AudioManager.Instance?.PlayButtonAudio();
        SettingsModal.Instance?.Show();
    }

    public void OnSandboxButtonClick()
    {
        AudioManager.Instance?.PlayButtonAudio();
        HideHud();
        UiLoader.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToSandboxScreen();
        }), 0.6f));
    }

    public void InitPlayerName() {         
        var localizeEvent = playerName.transform.Find("PlayerNameText").GetComponent<LocalizeStringEvent>();
        localizeEvent.StringReference.Arguments = new object[] { GameConfig.Data.name };
        localizeEvent.RefreshString();
    }
    public void InitMenu()
    {
        menuButtons?.Show();
        InitPlayerName();
        playerName.Show();
        foreach(UiAnimator animator in logoAnimators)
        {
            animator.Show();
        }
    }

    public void OnRenameButtonClick()
    {

        foreach (UiAnimator animator in nameModalAnimators)
        {
            animator.Show();
        }
    }
}
