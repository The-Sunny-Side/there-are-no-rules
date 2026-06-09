using UnityEngine;
using UnityEngine.Events;

public class TriggerManager : MonoBehaviour
{
    public UnityEvent onTriggerEnter;

    void OnTriggerEnter(Collider other)
    {
        onTriggerEnter?.Invoke();
    }
}
