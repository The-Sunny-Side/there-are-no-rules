using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NameplateManager : MonoBehaviour
{
    [SerializeField] private List<TextMeshPro> namePanels = new List<TextMeshPro>();

    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
    }

    public void SetName(string name)
    {
        foreach (var panel in namePanels)
        {
            if(panel != null)
                panel.text = name;
        }
    }

    private void LateUpdate()
    {
        if (_cam == null)
            return;

        transform.forward = _cam.transform.forward;
    }
}
