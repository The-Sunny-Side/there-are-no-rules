using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class RaceSemaphore : MonoBehaviour
{
    [SerializeField] private List<GameObject> lights;
    [SerializeField] private TextMeshProUGUI label;

    private int currentCount=3;

    public void ActivateLight(int index)
    {
        lights[index].GetComponent<Image>().color = Color.red;
    }

    public void DeactivateLight(int index)
        {
            lights[index].GetComponent<Image>().color = Color.black;
            currentCount--;
            label.text = index.ToString();
    }

    public void GreenLights()
    {
        foreach (var light in lights)
        {
            light.GetComponent<Image>().color = Color.green;
        }

        label.text = "GOOO!!!";
    }

    public void Hide()
    {
        UiTransition[] transitions = GetComponentsInChildren<UiTransition>();
        foreach (var transition in transitions)
        {
            transition.Hide();
        }
    }
}
