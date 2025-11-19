using System;
using TMPro;
using UnityEngine;

public class VehicleSelectorManager : MonoBehaviour
{
    [SerializeField] private GameObject[] selectors;
    [SerializeField] private GameObject[] selectorsUI;
    [SerializeField] private String[] stepNames = { "Scegli la base", "Scegli il corpo", "Completa" };
    [SerializeField] private TMP_Text stepTitleText;
    [SerializeField] private GameObject NextElementButton;
    [SerializeField] private GameObject PreviousElementButton;
    [SerializeField] private GameObject NextStepButton;
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
        Composer c = Composer.GetComponent<Composer>();
        c.SaveVehiclePrefab();
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

            GameObject baseInstance = Instantiate(baseElement, transform);
            GameObject bodyInstance = Instantiate(bodyElement, transform);

            baseInstance.transform.SetParent(composerComponent.transform);
            bodyInstance.transform.SetParent(composerComponent.transform);
            composerComponent.baseElement = baseInstance;
            composerComponent.bodyElement = bodyInstance;
            composerComponent.AlignComponents();

        }
        


    }

}
