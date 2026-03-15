using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class AutoCameraSwitch : MonoBehaviour
{
    [SerializeField] private CinemachineCamera[] virtualCameras;
    [SerializeField] private float switchInterval = 3f;

    private int currentIndex = 0;

    void Start()
    {
        if (virtualCameras == null || virtualCameras.Length == 0)
        {
            Debug.LogError("Nessuna virtual camera assegnata!");
            return;
        }

        ActivateCamera(0);
        StartCoroutine(AutoSwitch());
    }

    private IEnumerator AutoSwitch()
    {
        while (true)
        {
            yield return new WaitForSeconds(switchInterval);
            currentIndex = (currentIndex + 1) % virtualCameras.Length;
            ActivateCamera(currentIndex);
        }
    }

    private void ActivateCamera(int index)
    {
        for (int i = 0; i < virtualCameras.Length; i++)
        {
            virtualCameras[i].Priority = (i == index) ? 10 : 0;
        }
    }
}