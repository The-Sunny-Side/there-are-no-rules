using System.Collections.Generic;
using UnityEngine;

public class MainSceneManager : MonoBehaviour
{
    [SerializeField] private UiAnimator menu;
    [SerializeField] private GameObject playPanel;
    [SerializeField] private UiAnimator menuButtons;
    [SerializeField] private Composer composer;

    public void Start()
    {
    
        Dictionary<string, VehicleEntry> list = VehicleManager.Instance?.LoadVehicleConfig();


        if (list.TryGetValue(VehicleElementsKeys.Body, out VehicleEntry bodyElement) && (bodyElement?.element != null) &&
             list.TryGetValue(VehicleElementsKeys.Base, out VehicleEntry baseElement) && baseElement?.element != null)
        {
            composer.baseElement = Instantiate(baseElement.element);
            composer.bodyElement = Instantiate(bodyElement.element);

            if (list.TryGetValue(VehicleElementsKeys.WeaponBack, out VehicleEntry backWeaponElement) && backWeaponElement?.element!=null)
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

    public void OnPlayButtonClick()
    {
        AudioManager.Instance?.PlayButtonAudio();
        string json = VehicleManager.Instance?.GetVehicleJson();

        if (string.IsNullOrWhiteSpace(json))
        {
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
        menuButtons.Hide();
        playPanel?.GetComponent<UiAnimator>()?.Hide();
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
        menuButtons.Hide();
        playPanel?.GetComponent<UiAnimator>()?.Hide();
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
        menuButtons.Hide();
        playPanel?.GetComponent<UiAnimator>()?.Hide();
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
        menuButtons?.Hide();
        playPanel?.GetComponent<UiAnimator>()?.Hide();
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
}
