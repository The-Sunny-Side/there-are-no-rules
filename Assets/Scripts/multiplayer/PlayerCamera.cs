using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(2000)]
public class PlayerCamera : MonoBehaviour
{
    public static PlayerCamera instance;

    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [SerializeField] private CinemachineBrain _brain;

    void Awake()
    {
        instance = this;
        ResolveBrain();
        EnsureManualUpdateMode();
    }

    public void SetTarget(Transform target)
    {
        if (_cinemachineCamera)
        {
            _cinemachineCamera.Target.TrackingTarget = target;
            _cinemachineCamera.PreviousStateIsValid = false;
        }
    }

    private void ResolveBrain()
    {
        if (_brain != null)
            return;

        if (Camera.main != null && Camera.main.TryGetComponent(out CinemachineBrain mainBrain))
        {
            _brain = mainBrain;
            return;
        }

        _brain = FindAnyObjectByType<CinemachineBrain>();
    }

    private void EnsureManualUpdateMode()
    {
        if (_brain == null)
            return;

        if (_brain.UpdateMethod != CinemachineBrain.UpdateMethods.ManualUpdate)
            _brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
    }

    void LateUpdate()
    {
        if (_brain == null)
            ResolveBrain();

        if (_brain == null)
            return;

        EnsureManualUpdateMode();
        _brain.ManualUpdate();
    }
}
