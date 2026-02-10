using System;
using TMPro;
using UnityEngine;

[System.Serializable]
public class HighlightableElement
{
    public GameObject element;
    public GameObject highlight;
}


public class VehicleSelectorManager : MonoBehaviour
{
    [SerializeField] private GameObject weaponSelector;
    [SerializeField] private HighlightableElement[] stepButtons;
    [SerializeField] private string[] stepNames = { "Scegli la base", "Scegli il corpo", "Scegli l'arma", "Completa" };
    [SerializeField] private TMP_Text stepTitleText;
    [SerializeField] private Composer Composer;
    [SerializeField] private GameObject[] weaponElements;
    [SerializeField] private GameObject[] baseElements;
    [SerializeField] private GameObject[] bodyElements;

    private int selectedBaseIndex = 0;
    private int selectedBodyIndex = 0;
    private int[] selectedWeaponsIndexes = { 0, 0, 0, 0 };

    public int stepIndex = 0;

    void Awake()
    {
        UpdateActiveSelector();
    }

    public void SetStep(int step)
    {
            stepButtons[stepIndex].highlight.SetActive(false);

            stepIndex = step;

            stepButtons[stepIndex].highlight.SetActive(true);

            UpdateActiveSelector();
    }

    public void NextElement()
    {
        switch(stepIndex)
        {
            case 0:
                selectedBaseIndex = (selectedBaseIndex + 1) % baseElements.Length;
                break;
            case 1:
                selectedBodyIndex = (selectedBodyIndex + 1) % bodyElements.Length;
                break;
            case 2:
                weaponSelector.GetComponent<VehicleArmorsChooser>().NextElement();
                break;
        }

        UpdateActiveSelector();
    }
    public void PreviousElement()
    {
        switch (stepIndex)
        {
            case 0:
                selectedBaseIndex = (selectedBaseIndex - 1 + baseElements.Length) % baseElements.Length;
                break;
            case 1:
                selectedBodyIndex = (selectedBodyIndex - 1 + bodyElements.Length) % bodyElements.Length;
                break;
            case 2:
                weaponSelector.GetComponent<VehicleArmorsChooser>().PreviousElement();
                break;
        }

        UpdateActiveSelector();
    }
    public void FinalizeVehicle()
    {
        GameObject baseElement = baseElements[selectedBaseIndex];
        GameObject bodyElement = bodyElements[selectedBodyIndex];
        GameObject[] armors = weaponSelector.GetComponent<VehicleArmorsChooser>().selectedArmors;
        VehicleManager.Instance.SaveVehicleData(baseElement, bodyElement, armors);
    }
    public void UpdateActiveSelector()
    {
        weaponSelector.SetActive(stepIndex==2);

        Debug.Log("trigger");
        stepTitleText.text = stepNames[stepIndex];

            Composer composerComponent = Composer.GetComponent<Composer>();
        Transform composedVehicle = composerComponent.transform.Find("Vehicle");

        foreach (Transform child in composedVehicle.transform)
        {
            Destroy(child.gameObject);
        }

        GameObject baseElement = baseElements[selectedBaseIndex];
            GameObject bodyElement = bodyElements[selectedBodyIndex];
            GameObject[] armorElements = weaponSelector.GetComponent<VehicleArmorsChooser>().GetComponent<VehicleArmorsChooser>().selectedArmors;

            GameObject baseInstance = Instantiate(baseElement, transform);
            GameObject bodyInstance = Instantiate(bodyElement, transform);

            composerComponent.baseElement = baseInstance;
            composerComponent.bodyElement = bodyInstance;

            composerComponent.armorFrontElement = Instantiate(armorElements[0], transform);
            composerComponent.armorLeftElement = Instantiate(armorElements[1], transform);
            composerComponent.armorBackElement = Instantiate(armorElements[2], transform);
            composerComponent.armorRightElement = Instantiate(armorElements[3], transform);

            composerComponent.AlignComponents();

    }

    public void ResetVehicleRotation()
    {
        Transform composedVehicle = Composer.transform.Find("Vehicle");
        composedVehicle.rotation = Quaternion.identity;
    }

}
