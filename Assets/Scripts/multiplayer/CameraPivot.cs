using UnityEngine;

public class CameraPivot : MonoBehaviour
{
    private Transform _visuals;
    private bool _grounded = true;
    private float _frozenYaw;

    public void Init(Transform visuals)
    {
        _visuals = visuals;
    }

    public void SetGrounded(bool grounded)
    {
        if (!grounded && _grounded)
            _frozenYaw = _visuals.eulerAngles.y;

        _grounded = grounded;
    }

    public void Tick()
    {
        if (_visuals == null) return;

        transform.position = _visuals.position;

        float yaw = _grounded ? _visuals.eulerAngles.y : _frozenYaw;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}
