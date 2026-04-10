using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(2000)]
public class PlayerCamera : MonoBehaviour
{
    public static PlayerCamera instance;

    [SerializeField] private CinemachineCamera _onGroundCamera;
    [SerializeField] private CinemachineCamera _onAirCamera;
    [SerializeField] private CinemachineBrain _brain;

    void Awake()
    {
        instance = this;
        ResolveBrain();
        EnsureManualUpdateMode();
    }

    public void SetTarget(Transform target)
    {
        var cameras = GetComponentsInChildren<CinemachineCamera>();
        Debug.Log($"[PlayerCamera] SetTarget chiamato su '{gameObject.name}', trovate {cameras.Length} CinemachineCamera nei figli, target={target?.name}");
        foreach (var cam in cameras)
        {
            Debug.Log($"[PlayerCamera] -> Assegno target a '{cam.gameObject.name}'");
            cam.Target.TrackingTarget = target;
            cam.PreviousStateIsValid = false;
        }
    }

    public void SwitchCamera(bool grounded)
    {
        if (_onGroundCamera)
            _onGroundCamera.Priority = grounded ? 20 : 10;
        if (_onAirCamera)
            _onAirCamera.Priority = grounded ? 10 : 20;
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
