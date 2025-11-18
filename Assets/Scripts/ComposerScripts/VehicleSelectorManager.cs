using System;
using TMPro;
using UnityEngine;

public class VehicleSelectorManager : MonoBehaviour
{
    [SerializeField] private GameObject[] selectors;
    [SerializeField] private String[] stepNames = { "Scegli la base", "Scegli il corpo","Completa" };
    [SerializeField] private TMP_Text stepTitleText;
    [SerializeField] private GameObject NextElementButton;
    [SerializeField] private GameObject PreviousElementButton;
    [SerializeField] private GameObject Composer;

    public int stepIndex = 0;

    void Awake()
    {
        UpdateActiveSelector();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NextStep()
    {
        if (stepIndex == 0)
        {
            stepIndex++;
            UpdateActiveSelector();

        }
        else if (stepIndex == 1)
        {
            stepIndex++;
            UpdateActiveSelector();


        }
        else if (stepIndex == 2)
        {
            Composer.SetActive(true);
            Debug.Log("veicolo finito");

        }
    }

    public void PreviusStep()
    {
        if (stepIndex == 1)
        {
            stepIndex--;
            UpdateActiveSelector();
        }
        else if(stepIndex==2)
        {
            stepIndex--;
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
    private void UpdateActiveSelector()
    {
        if (stepIndex == 2)
        {
            for (int i = 0; i < selectors.Length; i++)
                selectors[i].SetActive(false);

            NextElementButton.SetActive(false);
            PreviousElementButton.SetActive(false);

            Composer.SetActive(true);
            Composer c = Composer.GetComponent<Composer>();
            GameObject baseElement = selectors[0].GetComponent<VehicleElementChooser>().selectedElement;
            GameObject bodyElement = selectors[1].GetComponent<VehicleElementChooser>().selectedElement;

            GameObject baseInstance = Instantiate(baseElement, transform);
            GameObject bodyInstance = Instantiate(bodyElement, transform);

            baseInstance.transform.SetParent(c.transform);
            bodyInstance.transform.SetParent(c.transform);
            c.baseElement= baseInstance;
            c.bodyElement= bodyInstance;
            c.AlignComponents();
            c.SaveVehiclePrefab();
        }
        else
        {
            NextElementButton.SetActive(true);
            PreviousElementButton.SetActive(true);
            for (int i = 0; i < selectors.Length; i++)
                selectors[i].SetActive(i == stepIndex);
        }

        stepTitleText.text = stepNames[stepIndex];
        
    }

}
