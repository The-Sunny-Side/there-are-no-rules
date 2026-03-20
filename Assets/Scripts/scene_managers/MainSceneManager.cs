using NUnit.Framework;
using System;
using System.Collections;
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
    
        Dictionary<string, GameObject> list = VehicleManager.Instance?.LoadVehicleData();


        if (list.TryGetValue(VehicleElementsKeys.Body, out var bodyPrefab) && bodyPrefab &&
             list.TryGetValue(VehicleElementsKeys.Base, out var basePrefab) && basePrefab)
        {
            composer.baseElement = Instantiate(basePrefab);
            composer.bodyElement = Instantiate(bodyPrefab);

            if (list.TryGetValue(VehicleElementsKeys.WeaponBack, out var back) && back)
                composer.weaponBackElement = Instantiate(back);

            if (list.TryGetValue(VehicleElementsKeys.WeaponFront, out var front) && front)
                composer.weaponFrontElement = Instantiate(front);

            if (list.TryGetValue(VehicleElementsKeys.WeaponLeft, out var left) && left)
                composer.weaponLeftElement = Instantiate(left);

            if (list.TryGetValue(VehicleElementsKeys.WeaponRight, out var right) && right)
                composer.weaponRightElement = Instantiate(right);

            composer.AlignComponents();
        }
    }

    public void OnPlayButtonClick()
    {
        GameManager.Instance?.SetNetworkMode(Mode.Client);
        AudioManager.Instance?.PlayOneShot("notification_ok");
        string json = VehicleManager.Instance?.GetVehicleJson();

        if (string.IsNullOrWhiteSpace(json))
        {
            UiLoader.Instance?.Show();
            StartCoroutine(Utilities.DelayedEvent((() =>
            {
                GameManager.Instance?.LoadSceneAsync("VehicleSelectionScene");
            })));

            return;
        }

        playPanel?.GetComponent<FadeAnimator>()?.Show();
    }

    public void OnPlayAsHost()
    {
        menuButtons.Hide();
        playPanel?.GetComponent<FadeAnimator>()?.Hide();
        GameManager.Instance.SetNetworkMode(Mode.Host);
        GameManager.Instance.SetIpAddress(Utilities.GetLocalIPAddress());
        UiLoader.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.LoadSceneAsync("multiplayerMovement");
        }), 0.6f));
    }

    public void OnPlayAsClient()
    {
        menuButtons.Hide();
        playPanel?.GetComponent<FadeAnimator>()?.Hide();
        GameManager.Instance?.SetNetworkMode(Mode.Client);
        GameManager.Instance?.SetIpAddress(Utilities.GetLocalIPAddress());
        UiLoader.Instance?.Show();
        UiLoader.Instance.setNoHiding(true);
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.LoadSceneAsync("LocalLobbyLoadingScene");
        }), 0.6f));
    }

    public void OnStartServerOnly()
    {
        menuButtons.Hide();
        playPanel?.GetComponent<FadeAnimator>()?.Hide();
        GameManager.Instance?.SetIpAddress(Utilities.GetLocalIPAddress());
        GameManager.Instance?.SetNetworkMode(Mode.ServerOnly);
        UiLoader.Instance?.Show();
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.LoadSceneAsync("multiplayerMovement");
        }), 0.6f));
    }

    public void OnVehicleSelectionButtonClick()
    {
        menuButtons?.Hide();
        playPanel?.GetComponent<FadeAnimator>()?.Hide();
        UiLoader.Instance?.Show();
        AudioManager.Instance?.PlayOneShot("notification_ok");
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.LoadSceneAsync("VehicleSelectionScene");
        }), 0.6f));
    }

    public void OnExitButtonClick()
    {
        AudioManager.Instance?.PlayOneShot("notification_ok");
        GameManager.Instance?.ExitGame();
    }

    public void OnSettingsButtonClick()
    {
        AudioManager.Instance?.PlayOneShot("notification_ok");
        SettingsModal.Instance?.Show();
    }
}
