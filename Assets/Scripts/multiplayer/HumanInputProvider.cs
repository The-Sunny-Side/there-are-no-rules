using UnityEngine;

public class HumanInputProvider : MonoBehaviour, IVehicleInputProvider
{
    private MobileInputManager _im;

    void Awake()
    {
        _im = MobileInputManager.instance;
    }

    public float Steer => _im != null ? _im.rotateHorizontal : 0f;
    public float Throttle => _im != null ? _im.rotateVertical : 0f;
    public bool JumpTapped => _im != null && _im.jumpTapped;

    public bool UseFront => _im != null && _im.weaponSelectAxis.y > 0f;
    public bool UseBack => _im != null && _im.weaponSelectAxis.y < 0f;
    public bool UseLeft => _im != null && _im.weaponSelectAxis.x < 0f;
    public bool UseRight => _im != null && _im.weaponSelectAxis.x > 0f;
}
