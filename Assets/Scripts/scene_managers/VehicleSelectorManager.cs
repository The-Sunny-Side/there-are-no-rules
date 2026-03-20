using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static VehiclePrefabRegistry;

public class VehicleSelectorManager : MonoBehaviour
{
    [SerializeField] private Composer Composer;
    [SerializeField] private UiAnimator elementsPanel;
    [SerializeField] private UiAnimator stepsBar;
    [SerializeField] private UiAnimator weaponsPanel;
    [SerializeField] private UiAnimator topButtons;
    [SerializeField] private GameObject vehicle;
    [SerializeField] private GameObject[] elementsBoxes;
    [SerializeField] private GameObject[] stepButtons;

    
    /** 0 = Top - 1 = Left - 2 = Back - 3 = Right **/
    [SerializeField] private int selectedWeaponType = 0;
    [SerializeField] private GameObject[] selectedWeapons;
    [SerializeField] private VehicleEntry[] selectedWeaponsElements;
    [SerializeField] private GameObject[] weaponButtons;
    [SerializeField] private bool isVisible = true;


    private VehicleEntry[] weaponElements;
    private VehicleEntry[] baseElements;
    private VehicleEntry[] bodyElements;
    private Sprite emptyPart;
    private int stepIndex = 0;
    private int[] selectedWeaponsIndexes = new int[4] { 0, 0, 0, 0 };
    private int selectedBaseIndex = 0;
    private int selectedBodyIndex = 0;
    private Image[] weaponIcons = new Image[4];

    void Awake()
    {
        emptyPart=GameConfig.VehiclePrefabRegistry.EmptyPart.icon;
        baseElements = GameConfig.VehiclePrefabRegistry.GetAllBases().ToArray();
        bodyElements = GameConfig.VehiclePrefabRegistry.GetAllBodies().ToArray();
        weaponElements = GameConfig.VehiclePrefabRegistry.GetAllWeapons().ToArray();

        for (int i = 0; i < weaponButtons.Length; i++)
        {
            GameObject buttonObj = weaponButtons[i].transform.Find("IconBox").Find("Icon").gameObject;
            weaponIcons[i] = buttonObj.GetComponent<Image>();
        }

        Dictionary<string, GameObject> loaded = VehicleManager.Instance?.LoadVehicleData();

        if (loaded != null)
        {
            if (loaded.ContainsKey(VehicleElementsKeys.Base))
            {
                int loadedBaseIndex = Array.FindIndex(baseElements, e => e.element.name == loaded[VehicleElementsKeys.Base].name);
                if (loadedBaseIndex != -1)
                {
                    selectedBaseIndex = loadedBaseIndex;
                }
            }

            if (loaded.ContainsKey(VehicleElementsKeys.Body))
            {
                int loadedBodyIndex = Array.FindIndex(bodyElements, e => e.element.name == loaded[VehicleElementsKeys.Body].name);
                if (loadedBodyIndex != -1)
                {
                    selectedBodyIndex = loadedBodyIndex;
                }
            }

            if (loaded.ContainsKey(VehicleElementsKeys.WeaponFront))
            {
                int loadedWeaponIndex = Array.FindIndex(weaponElements, e => e.element.name == loaded[VehicleElementsKeys.WeaponFront].name);
                if (loadedWeaponIndex != -1)
                {
                    selectedWeaponsIndexes[0] = loadedWeaponIndex;
                    selectedWeapons[0] = weaponElements[loadedWeaponIndex].element;
                    selectedWeaponsElements[0] = weaponElements[loadedWeaponIndex];
                }
            }

            if (loaded.ContainsKey(VehicleElementsKeys.WeaponLeft))
            {
                int loadedWeaponIndex = Array.FindIndex(weaponElements, e => e.element.name == loaded[VehicleElementsKeys.WeaponLeft].name);
                if (loadedWeaponIndex != -1)
                {
                    selectedWeaponsIndexes[1] = loadedWeaponIndex;
                    selectedWeapons[1] = weaponElements[loadedWeaponIndex].element;
                    selectedWeaponsElements[1] = weaponElements[loadedWeaponIndex];
                }
            }

            if (loaded.ContainsKey(VehicleElementsKeys.WeaponBack))
            {
                int loadedWeaponIndex = Array.FindIndex(weaponElements, e => e.element.name == loaded[VehicleElementsKeys.WeaponBack].name);
                if (loadedWeaponIndex != -1)
                {
                    selectedWeaponsIndexes[2] = loadedWeaponIndex;
                    selectedWeapons[2] = weaponElements[loadedWeaponIndex].element;
                    selectedWeaponsElements[2] = weaponElements[loadedWeaponIndex];
                }
            }

            if (loaded.ContainsKey(VehicleElementsKeys.WeaponRight))
            {
                int loadedWeaponIndex = Array.FindIndex(weaponElements, e => e.element.name == loaded[VehicleElementsKeys.WeaponRight].name);
                if (loadedWeaponIndex != -1)
                {
                    selectedWeaponsIndexes[3] = loadedWeaponIndex;
                    selectedWeapons[3] = weaponElements[loadedWeaponIndex].element;
                    selectedWeaponsElements[3] = weaponElements[loadedWeaponIndex];
                }
            }
        }

        for (int i = 0; i < weaponButtons.Length; i++)
        {
            weaponIcons[i].sprite = selectedWeaponsElements[i].icon;
        }

        StartCoroutine(Utilities.DelayedEvent(() =>
        {
            SetHudVisibility(true);
        }, 0.1f));

        SetStep(0);
    }

    public void InitIcons()
    {
        VehicleElement[] actualParts = { };
        int partIndex = 0;

        switch (stepIndex)
        {
            case 0:
                actualParts = baseElements;
                partIndex = selectedBaseIndex;
                break;
            case 1:
                actualParts = bodyElements;
                partIndex = selectedBodyIndex;
                break;
            case 2:
                actualParts = weaponElements;
                partIndex = selectedWeaponsIndexes[selectedWeaponType];
                break;
        }

        for (int i = 0; i < elementsBoxes.Length; i++)
        {
            HighlightButton(elementsBoxes[i], false);

            if (i < actualParts.Length)
            {
                elementsBoxes[i].transform.Find("Icon").GetComponent<Image>().sprite = actualParts[i].icon;
            }
            else
                elementsBoxes[i].transform.Find("Icon").GetComponent<Image>().sprite = emptyPart;
        }
        HighlightButton(elementsBoxes[partIndex]);
    }

    public void HighlightButton(GameObject button, bool highlight = true)
    {
        button.GetComponent<Image>().color = highlight ? GameConfig.UiConfig.selectedColor : GameConfig.UiConfig.baseColor;
    }


    public void SetStep(int step)
    {
        HighlightButton(stepButtons[stepIndex].gameObject, false);
        stepIndex = step;
        HighlightButton(stepButtons[stepIndex].gameObject);

        switch (step)
        {
            case 0:
                {
                    weaponsPanel.Hide();
                }
                break;
            case 1:
                {
                    weaponsPanel.Hide();
                }
                break;
            case 2:
                {
                    HighlightButton(weaponButtons[selectedWeaponType], true);
                    weaponsPanel.Show();
                }
                break;
        }

        InitIcons();
        UpdateVehicle();
    }

    public void SetHudVisibility(bool visible)
    {
        isVisible = visible;
        if (isVisible)
        {
            elementsPanel.Show();
            stepsBar.Show();
            if (stepIndex == 2)
                weaponsPanel.Show();
        }
        else
        {
            elementsPanel.Hide();
            stepsBar.Hide();
            weaponsPanel.Hide();
        }
    }

    public void ToggleHudVisibility()
    {
        SetHudVisibility(!isVisible);
    }

    public void SetElement(int elementIndex)
    {

        int indexToDisable = 0;

        switch (stepIndex)
        {
            case 0:
                {
                    if (elementIndex >= baseElements.Length) return;
                    indexToDisable = selectedBaseIndex;
                    selectedBaseIndex = elementIndex;
                }
                break;
            case 1:
                {
                    if (elementIndex >= bodyElements.Length) return;
                    indexToDisable = selectedBodyIndex;
                    selectedBodyIndex = elementIndex;
                }
                break;
            case 2:
                {
                    if (elementIndex >= weaponElements.Length) return;
                    indexToDisable = selectedWeaponsIndexes[selectedWeaponType];
                    weaponIcons[selectedWeaponType].sprite = weaponElements[elementIndex].icon;
                    selectedWeapons[selectedWeaponType] = weaponElements[elementIndex].element;
                    selectedWeaponsElements[selectedWeaponType] = weaponElements[elementIndex];
                    selectedWeaponsIndexes[selectedWeaponType] = elementIndex;
                }
                break;
        }

        HighlightButton(elementsBoxes[indexToDisable], false);
        HighlightButton(elementsBoxes[elementIndex]);

        UpdateVehicle();
    }

    public void SetWeaponType(int type)
    {
        HighlightButton(weaponButtons[selectedWeaponType], false);
        HighlightButton(elementsBoxes[selectedWeaponsIndexes[selectedWeaponType]], false);
        selectedWeaponType = type;
        HighlightButton(weaponButtons[selectedWeaponType], true);
        HighlightButton(elementsBoxes[selectedWeaponsIndexes[selectedWeaponType]], true);
        UpdateVehicle();
    }

    public void NextStep()
    {
        if (stepIndex < 2)
            SetStep(stepIndex + 1);
    }

    public void PrevStep()
    {
        if (stepIndex > 0)
            SetStep(stepIndex - 1);
    }

    public void FinalizeVehicle()
    {
        AudioManager.Instance?.PlayOneShot("notification_ok");
        SetHudVisibility(false);
        topButtons.Hide();

        UiLoader.Instance?.Show();

        VehicleEntry baseElement = baseElements[selectedBaseIndex];
        VehicleEntry bodyElement = bodyElements[selectedBodyIndex];
        VehicleManager.Instance?.SaveVehicleData(baseElement, bodyElement, selectedWeaponsElements);
        StartCoroutine(Utilities.DelayedEvent((() =>
        {
            GameManager.Instance?.GoToHomeScreen();
        }), 0.6f));
    }

    private void HighlightSelectedPart(GameObject part, bool active = true)
    {
        PulsingHighlight mr = part.GetComponent<PulsingHighlight>();

        mr.enabled = active;
    }

    public void UpdateVehicle()
    {


        Composer composerComponent = Composer.GetComponent<Composer>();
        Transform composedVehicle = composerComponent.transform.Find("Vehicle");

        Utilities.DestroyAllChildren(composedVehicle);

        GameObject baseElement = baseElements[selectedBaseIndex].element;
        GameObject bodyElement = bodyElements[selectedBodyIndex].element;

        GameObject baseInstance = Instantiate(baseElement, transform);
        GameObject bodyInstance = Instantiate(bodyElement, transform);


        composerComponent.baseElement = baseInstance;
        composerComponent.bodyElement = bodyInstance;

        composerComponent.weaponFrontElement = Instantiate(selectedWeapons[0], transform);
        composerComponent.weaponLeftElement = Instantiate(selectedWeapons[1], transform);
        composerComponent.weaponBackElement = Instantiate(selectedWeapons[2], transform);
        composerComponent.weaponRightElement = Instantiate(selectedWeapons[3], transform);

        composerComponent.AlignComponents();
        vehicle.GetComponent<ObjectRotator>()?.SetupCollider();

        switch (stepIndex)
        {
            case 0:
                HighlightSelectedPart(baseInstance);
                break;
            case 1:
                HighlightSelectedPart(bodyInstance);
                break;
            case 2:
                GameObject[] weaponsObjects = new GameObject[] {
                    composerComponent.weaponFrontElement,
                    composerComponent.weaponLeftElement,
                    composerComponent.weaponBackElement,
                    composerComponent.weaponRightElement
                };
                GameObject selectedWeaponPart = weaponsObjects[selectedWeaponType];
                HighlightSelectedPart(selectedWeaponPart);
                selectedWeaponPart.GetComponent<VehicleWeapon>()?.ActivateWeapon();
                break;

        }

    }

    public void ResetVehicleRotation()
    {
        Transform composedVehicle = Composer.transform.Find("Vehicle");
        composedVehicle.rotation = Quaternion.identity;
    }

}
