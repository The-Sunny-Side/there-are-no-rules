using System;
using TMPro;
using UnityEngine;

public class VehicleSelectorManager : MonoBehaviour
{
    [SerializeField] private GameObject[] selectors;
    [SerializeField] private GameObject[] selectorsUI;
    [SerializeField] private String[] stepNames = { "Scegli la base", "Scegli il corpo", "Scegli l'arma", "Completa" };
    [SerializeField] private TMP_Text stepTitleText;
    [SerializeField] private GameObject NextElementButton;
    [SerializeField] private GameObject PreviousElementButton;
    [SerializeField] private GameObject NextStepButton;
    [SerializeField] private GameObject PrevStepButton;
    [SerializeField] private GameObject FinalizzaButton;
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

        VehicleManager.Instance.SaveVehicleData(baseElement, bodyElement);
    }
    private void UpdateActiveSelector()
    {
        for (int i = 0; i < selectors.Length; i++)
        {
            selectors[i].SetActive(i == stepIndex);

            selectorsUI[i].SetActive(i == stepIndex);
        }

        stepTitleText.text = stepNames[stepIndex];

        switch (stepIndex)
        {
            case 0:
                {
                    PrevStepButton.SetActive(false);
                    FinalizzaButton.SetActive(false);
                    NextStepButton.SetActive(true);
                    PreviousElementButton.SetActive(true);
                    NextElementButton.SetActive(true);
                }
                break;

            case 1:
                {
                    PrevStepButton.SetActive(true);
                    NextStepButton.SetActive(true);
                    FinalizzaButton.SetActive(false);
                    PreviousElementButton.SetActive(true);
                    NextElementButton.SetActive(true);
                }
                break;
            case 2:
                {
                    PrevStepButton.SetActive(true);
                    NextStepButton.SetActive(true);
                    FinalizzaButton.SetActive(false);
                    PreviousElementButton.SetActive(true);
                    NextElementButton.SetActive(true);
                }
                break;
            case 3:
                {
                    PreviousElementButton.SetActive(false);
                    NextElementButton.SetActive(false);
                    NextStepButton.SetActive(false);
                    FinalizzaButton.SetActive(true);
                }
                break;

        }

        if (Composer.activeInHierarchy)
        {

            Composer composerComponent = Composer.GetComponent<Composer>();
            GameObject baseElement = selectors[0].GetComponent<VehicleElementChooser>().selectedElement;
            GameObject bodyElement = selectors[1].GetComponent<VehicleElementChooser>().selectedElement;
            GameObject armorElement = selectors[2].GetComponent<VehicleElementChooser>().selectedElement;

            GameObject baseInstance = Instantiate(baseElement, transform);
            GameObject bodyInstance = Instantiate(bodyElement, transform);
            GameObject armorInstance = Instantiate(armorElement, transform);

            baseInstance.transform.SetParent(composerComponent.transform);
            bodyInstance.transform.SetParent(composerComponent.transform);
            composerComponent.baseElement = baseInstance;
            composerComponent.bodyElement = bodyInstance;
            composerComponent.armorRightElement = armorInstance;
            composerComponent.AlignComponents();

        }

    }

}
