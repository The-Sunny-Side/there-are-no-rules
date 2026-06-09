using UnityEngine;
using UnityEngine.Events;

public class InfoMarker : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private UnityEvent OnActivate;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerSphere"))
        {
            OnActivate.Invoke();
        }
    }
}

