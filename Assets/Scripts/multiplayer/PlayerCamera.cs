using Unity.Cinemachine;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public static PlayerCamera instance;

    [SerializeField] private CinemachineCamera _cinemachineCamera;

    void Awake()
    {
        instance = this;
    }

    public void SetTarget(Transform target)
    {
        if (_cinemachineCamera)
        {
            _cinemachineCamera.Target.TrackingTarget = target;
        }
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
