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
    [SerializeField] private GameObject[] selectors;
    [SerializeField] private GameObject[] selectorsUI;
    [SerializeField] private HighlightableElement[] stepButtons;
    [SerializeField] private String[] stepNames = { "Scegli la base", "Scegli il corpo", "Scegli l'arma", "Completa" };
    [SerializeField] private TMP_Text stepTitleText;
    [SerializeField] private GameObject NextElementButton;
    [SerializeField] private GameObject PreviousElementButton;
    [SerializeField] private GameObject Composer;

    public int stepIndex = 0;

    void Awake()
    {
        UpdateActiveSelector();
    }

    public void NextStep()
    {
        stepIndex++;
        UpdateActiveSelector();
    }

    public void PreviusStep()
    {
        stepIndex--;
        UpdateActiveSelector();
    }

    public void SetStep(int step)
    {
        if(step>= 0 && step < selectors.Length)
        {
            stepButtons[stepIndex].highlight.SetActive(false);

            stepIndex = step;

            stepButtons[stepIndex].highlight.SetActive(true);

            UpdateActiveSelector();
        }
    }

    public void NextElement()
    {
        selectors[stepIndex].GetComponent<VehicleElementChooser>().NextElement();
    }
    public void PreviousElement()
    {
        selectors[stepIndex].GetComponent<VehicleElementChooser>().PreviousElement();
    }
    public void FinalizeVehicle()
    {
        GameObject baseElement = selectors[0].GetComponent<VehicleElementChooser>().selectedElement;
        GameObject bodyElement = selectors[1].GetComponent<VehicleElementChooser>().selectedElement;
        GameObject[] armors = selectors[2].GetComponent<VehicleArmorsChooser>().selectedArmors;
        VehicleManager.Instance.SaveVehicleData(baseElement, bodyElement, armors);
    }
    private void UpdateActiveSelector()
    {
        for (int i = 0; i < selectors.Length; i++)
        {
            selectors[i].SetActive(i == stepIndex);

            selectorsUI[i].SetActive(i == stepIndex);
        }

        stepTitleText.text = stepNames[stepIndex];

        if (Composer.activeInHierarchy)
        {

            Composer composerComponent = Composer.GetComponent<Composer>();
            GameObject baseElement = selectors[0].GetComponent<VehicleElementChooser>().selectedElement;
            GameObject bodyElement = selectors[1].GetComponent<VehicleElementChooser>().selectedElement;
            GameObject[] armorElements = selectors[2].GetComponent<VehicleArmorsChooser>().selectedArmors;

            GameObject baseInstance = Instantiate(baseElement, transform);
            GameObject bodyInstance = Instantiate(bodyElement, transform);



            baseInstance.transform.SetParent(composerComponent.transform);
            bodyInstance.transform.SetParent(composerComponent.transform);
            composerComponent.baseElement = baseInstance;
            composerComponent.bodyElement = bodyInstance;

            composerComponent.armorFrontElement = Instantiate(armorElements[0], transform);
            composerComponent.armorLeftElement = Instantiate(armorElements[1], transform);
            composerComponent.armorBackElement = Instantiate(armorElements[2], transform);
            composerComponent.armorRightElement = Instantiate(armorElements[3], transform);

            composerComponent.AlignComponents();

        }

    }

}
