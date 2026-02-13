using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VehicleSelectorManager : MonoBehaviour
{
    [SerializeField] private Composer Composer;

    [SerializeField] private UiAnimator prevStepButton;
    [SerializeField] private UiAnimator nextStepButton;
    [SerializeField] private UiAnimator elementsPanel;
    [SerializeField] private UiAnimator stepsBar;
    [SerializeField] private UiAnimator weaponsPanel;
    [SerializeField] private UiAnimator vehicle;

    [SerializeField] private HighlightableElement[] stepButtons;

    [SerializeField] private GameObject[] elementsBoxes;

    [SerializeField] private VehicleElement[] weaponElements;
    [SerializeField] private VehicleElement[] baseElements;
    [SerializeField] private VehicleElement[] bodyElements;

    [SerializeField] private Sprite emptyPart;

    /** 0 = Top - 1 = Left - 2 = Back - 3 = Right **/
    [SerializeField] private int selectedWeaponType = 0;
    [SerializeField] private GameObject[] selectedWeapons;
    [SerializeField] private GameObject[] weaponButtons;
    [SerializeField] private bool isVisible = true;


    private int stepIndex = 0;
    private int[] selectedWeaponsIndexes = new int[4] { 0, 0, 0, 0 };
    private int selectedBaseIndex = 0;
    private int selectedBodyIndex = 0;

    void Awake()
    {
        Dictionary<string, GameObject> loaded = VehicleManager.Instance.LoadVehicleData();

        if (loaded != null)
        {
            if (loaded.ContainsKey("base"))
            {
                int loadedBaseIndex = Array.FindIndex(baseElements, e => e.element.name == loaded["base"].name);
                if (loadedBaseIndex != -1)
                {
                    selectedBaseIndex = loadedBaseIndex;
                }
            }

            if (loaded.ContainsKey("body"))
            {
                int loadedBodyIndex = Array.FindIndex(bodyElements, e => e.element.name == loaded["body"].name);
                if (loadedBodyIndex != -1)
                {
                    selectedBodyIndex = loadedBodyIndex;
                }
            }
        }

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
            elementsBoxes[i].GetComponent<Outline>().enabled = false;

            if (i < actualParts.Length)
            {
                elementsBoxes[i].transform.Find("Icon").GetComponent<Image>().sprite = actualParts[i].icon;
            }
            else
                elementsBoxes[i].transform.Find("Icon").GetComponent<Image>().sprite = emptyPart;
        }

        elementsBoxes[partIndex].GetComponent<Outline>().enabled = true;
    }

    public void SetStep(int step)
    {
        stepButtons[stepIndex].highlight.SetActive(false);
        stepIndex = step;
        stepButtons[stepIndex].highlight.SetActive(true);

        switch (step)
        {
            case 0:
                {
                    prevStepButton.Hide();
                    nextStepButton.Show();
                    weaponsPanel.Hide();
                }
                break;
            case 1:
                {
                    prevStepButton.Show();
                    nextStepButton.Show();
                    weaponsPanel.Hide();
                }
                break;
            case 2:
                {
                    prevStepButton.Show();
                    nextStepButton.Hide();
                    weaponsPanel.Show();
                }
                break;
        }

        InitIcons();
        UpdateActiveSelector();
    }

    public void ToggleHudVisibility()
    {
        isVisible = !isVisible;
        if (isVisible)
        {
            vehicle.Show();
            elementsPanel.Show();
            stepsBar.Show();
            if (stepIndex==2)
                weaponsPanel.Show();
        }
        else
        {
            vehicle.Hide();
            elementsPanel.Hide();
            stepsBar.Hide();
            weaponsPanel.Hide();
        }
    }

    public void SetElement(int elementIndex)
    {

        switch (stepIndex)
        {
            case 0:
                {
                    if (elementIndex >= baseElements.Length) return;
                    elementsBoxes[selectedBaseIndex].GetComponent<Outline>().enabled = false;
                    selectedBaseIndex = elementIndex;
                    elementsBoxes[elementIndex].GetComponent<Outline>().enabled = true;
                }
                break;
            case 1:
                {
                    if (elementIndex >= bodyElements.Length) return;
                    elementsBoxes[selectedBodyIndex].GetComponent<Outline>().enabled = false;
                    selectedBodyIndex = elementIndex;
                    elementsBoxes[elementIndex].GetComponent<Outline>().enabled = true;
                }
                break;
            case 2:
                {
                    if (elementIndex >= weaponElements.Length) return;
                    elementsBoxes[selectedWeaponsIndexes[selectedWeaponType]].GetComponent<Outline>().enabled = false;
                    weaponButtons[selectedWeaponType].transform.Find("IconBox").Find("Icon").GetComponent<Image>().sprite = weaponElements[elementIndex].icon;
                    selectedWeapons[selectedWeaponType] = weaponElements[elementIndex].element;
                    selectedWeaponsIndexes[selectedWeaponType] = elementIndex;
                    elementsBoxes[selectedWeaponsIndexes[selectedWeaponType]].GetComponent<Outline>().enabled = true;
                }
                break;
        }

        UpdateActiveSelector();
    }

    public void SetWeaponType(int type)
    {
        elementsBoxes[selectedWeaponsIndexes[selectedWeaponType]].GetComponent<Outline>().enabled = false;
        weaponButtons[selectedWeaponType].GetComponent<Outline>().enabled = false;
        selectedWeaponType = type;
        weaponButtons[selectedWeaponType].GetComponent<Outline>().enabled = true;
        elementsBoxes[selectedWeaponsIndexes[selectedWeaponType]].GetComponent<Outline>().enabled = true;

        UpdateActiveSelector();
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
        GameObject baseElement = baseElements[selectedBaseIndex].element;
        GameObject bodyElement = bodyElements[selectedBodyIndex].element;
        VehicleManager.Instance.SaveVehicleData(baseElement, bodyElement, selectedWeapons);
    }

    private void HighlightSelectedPart(GameObject part, bool active=true)
    {
        PulsingHighlight mr = part.GetComponent<PulsingHighlight>();

        mr.enabled = active;
    }

    public void UpdateActiveSelector()
    {


        Composer composerComponent = Composer.GetComponent<Composer>();
        Transform composedVehicle = composerComponent.transform.Find("Vehicle");

        foreach (Transform child in composedVehicle.transform)
        {
            Destroy(child.gameObject);
        }

        GameObject baseElement = baseElements[selectedBaseIndex].element;
        GameObject bodyElement = bodyElements[selectedBodyIndex].element;

        GameObject baseInstance = Instantiate(baseElement, transform);
        GameObject bodyInstance = Instantiate(bodyElement, transform);


        composerComponent.baseElement = baseInstance;
        composerComponent.bodyElement = bodyInstance;

        composerComponent.armorFrontElement = Instantiate(selectedWeapons[0], transform);
        composerComponent.armorLeftElement = Instantiate(selectedWeapons[1], transform);
        composerComponent.armorBackElement = Instantiate(selectedWeapons[2], transform);
        composerComponent.armorRightElement = Instantiate(selectedWeapons[3], transform);

        composerComponent.AlignComponents();

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
                    composerComponent.armorFrontElement,
                    composerComponent.armorLeftElement,
                    composerComponent.armorBackElement,
                    composerComponent.armorRightElement
                };
                GameObject selectedWeaponPart = weaponsObjects[selectedWeaponType];
                HighlightSelectedPart(selectedWeaponPart);
                Animator animator = selectedWeaponPart.GetComponent<Animator>();
                if(animator != null)
                {
                    animator.SetTrigger("activate");
                }

                break;

        }

    }

    public void ResetVehicleRotation()
    {
        Transform composedVehicle = Composer.transform.Find("Vehicle");
        composedVehicle.rotation = Quaternion.identity;
    }

}
