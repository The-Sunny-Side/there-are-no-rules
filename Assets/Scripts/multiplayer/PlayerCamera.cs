using System.Collections;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

[DefaultExecutionOrder(2000)]
public class PlayerCamera : MonoBehaviour
{
    public static PlayerCamera instance;

    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [SerializeField] private CinemachineBrain _brain;

    private CinemachineOrbitalFollow _orbital;

    void Awake()
    {
        instance = this;
        if (_cinemachineCamera)
            _orbital = _cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
        ResolveBrain();
        EnsureManualUpdateMode();
    }

    public void SetTarget(Transform target)
    {
        if (_cinemachineCamera)
        {
            _cinemachineCamera.Target.TrackingTarget = target;
            StartCoroutine(ResetBindingMode());
        }
    }

    private IEnumerator ResetBindingMode()
    {
        if (_orbital == null)
            yield break;

        var original = _orbital.TrackerSettings.BindingMode;
        _orbital.TrackerSettings.BindingMode = BindingMode.LazyFollow;
        yield return null;
        _orbital.TrackerSettings.BindingMode = original;
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
