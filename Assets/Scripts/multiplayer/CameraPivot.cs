using UnityEngine;

public class CameraPivot : MonoBehaviour
{
    [SerializeField] private float landingSmoothTime = 0.2f;

    private Transform _visuals;
    private bool _grounded = true;
    private bool _blending;
    private float _frozenYaw;
    private float _blendOffset;
    private float _blendTimer;

    public void Init(Transform visuals)
    {
        _visuals = visuals;
    }

    public void SetGrounded(bool grounded)
    {
        if (!grounded && _grounded)
        {
            _frozenYaw = _blending
                ? VisualsYaw() + CurrentOffset()
                : VisualsYaw();
            _blending = false;
        }
        else if (grounded && !_grounded)
        {
            _blendOffset = Mathf.DeltaAngle(VisualsYaw(), _frozenYaw);
            _blendTimer = landingSmoothTime;
            _blending = true;
        }

        _grounded = grounded;
    }

    public void Tick()
    {
        if (_visuals == null) return;

        transform.position = _visuals.position;

        float yaw;
        if (!_grounded)
        {
            yaw = _frozenYaw;
        }
        else if (_blending)
        {
            _blendTimer -= Time.deltaTime;
            if (_blendTimer <= 0f)
            {
                _blending = false;
                yaw = VisualsYaw();
            }
            else
            {
                yaw = VisualsYaw() + CurrentOffset();
            }
        }
        else
        {
            yaw = VisualsYaw();
        }

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private float VisualsYaw()
    {
        Vector3 fwd = _visuals.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f)
            return transform.eulerAngles.y;
        return Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
    }

    private float CurrentOffset()
    {
        if (landingSmoothTime <= 0f) return 0f;
        float t = Mathf.Clamp01(_blendTimer / landingSmoothTime);
        return _blendOffset * Mathf.SmoothStep(0f, 1f, t);
    }
}
