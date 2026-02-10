using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class HighlightableElement
{
    public GameObject element;
    public GameObject highlight;
}

[Serializable]
public class VehicleElement
{
    public GameObject element;
    public Sprite icon;
}

public class VehicleSelectorManager : MonoBehaviour
{
    [SerializeField] private HighlightableElement[] stepButtons;
    [SerializeField] private Composer Composer;
    [SerializeField] private VehicleElement[] weaponElements;
    [SerializeField] private VehicleElement[] baseElements;
    [SerializeField] private VehicleElement[] bodyElements;
    [SerializeField] private GameObject[] elementsBoxes;
    [SerializeField] private Image[] elementsIconBoxes;
    [SerializeField] private Sprite emptyPart;

    /** 0 = Top - 1 = Left - 2 = Back - 3 = Right **/
    [SerializeField] private int selectedWeaponType = 0;
    public GameObject[] selectedWeapons;
    private int[] selectedWeaponsIndexes = new int[4] { 0, 0, 0, 0 };
    [SerializeField] private Image[] weaponIconBoxes;
    [SerializeField] private GameObject[] weaponButtons;
    [SerializeField] private GameObject prevStepButton;
    [SerializeField] private GameObject nextStepButton;
    [SerializeField] private GameObject weaponsPanel;


    public int stepIndex = 0;
    private int selectedBaseIndex = 0;
    private int selectedBodyIndex = 0;

    void Awake()
    {
        UpdateActiveSelector();
        InitIcons();
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

        InitIcons();
        UpdateActiveSelector();
    }

    public void SetElement(int elementIndex)
    {

        switch (stepIndex)
        {
            case 0:
                {
                    elementsBoxes[selectedBaseIndex].GetComponent<Outline>().enabled = false;
                    selectedBaseIndex = elementIndex;
                    elementsBoxes[elementIndex].GetComponent<Outline>().enabled = true;
                }
                break;
            case 1:
                {
                    elementsBoxes[selectedBodyIndex].GetComponent<Outline>().enabled = false;
                    selectedBodyIndex = elementIndex;
                    elementsBoxes[elementIndex].GetComponent<Outline>().enabled = true;
                }
                break;
            case 2:
                {
                    elementsBoxes[selectedWeaponsIndexes[selectedWeaponType]].GetComponent<Outline>().enabled = false;
                    weaponIconBoxes[selectedWeaponType].sprite = weaponElements[elementIndex].icon;
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
        weaponButtons[selectedWeaponType].GetComponent<Outline>().enabled = false;
        selectedWeaponType = type;
        weaponButtons[selectedWeaponType].GetComponent<Outline>().enabled = true;
    }

    public void NextStep()
    {
        if (stepIndex < 21)
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
    public void UpdateActiveSelector()
    {
        prevStepButton.SetActive(stepIndex > 0);
        nextStepButton.SetActive(stepIndex < 2);
        weaponsPanel.SetActive(stepIndex == 2);

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

    }

    public void ResetVehicleRotation()
    {
        Transform composedVehicle = Composer.transform.Find("Vehicle");
        composedVehicle.rotation = Quaternion.identity;
    }

}
