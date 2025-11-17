using System;
using TMPro;
using UnityEngine;

public class VehicleSelectorManager : MonoBehaviour
{
    [SerializeField] private GameObject[] selectors;
    [SerializeField] private String[] stepNames = { "Scegli la base", "Scegli il corpo" };
    [SerializeField] private TMP_Text stepTitleText; 

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
        stepTitleText.text = stepNames[stepIndex];
        for (int i = 0; i < selectors.Length; i++)
            selectors[i].SetActive(i == stepIndex);
    }

}
