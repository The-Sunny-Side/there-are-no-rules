using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(2000)]
public class PlayerCamera : MonoBehaviour
{
    public static PlayerCamera instance;

    [SerializeField] private CinemachineCamera _camera;
    [SerializeField] private CinemachineBrain _brain;

    private CameraPivot _pivot;

    void Awake()
    {
        instance = this;
        ResolveBrain();
        EnsureManualUpdateMode();
    }

    public void SetTarget(Transform target)
    {
        _pivot = target.GetComponent<CameraPivot>();

        if (_camera)
        {
            _camera.Target.TrackingTarget = target;
            _camera.PreviousStateIsValid = false;
        }
    }

    void LateUpdate()
    {
        if (_brain == null)
            ResolveBrain();

        if (_brain == null)
            return;

        EnsureManualUpdateMode();
        if (_pivot != null)
            _pivot.Tick();
        _brain.ManualUpdate();
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
}
