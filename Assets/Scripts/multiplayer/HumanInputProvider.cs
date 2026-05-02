using UnityEngine;

public class HumanInputProvider : MonoBehaviour, IVehicleInputProvider
{
    private MobileInputManager _im;

    void Awake()
    {
        _im = MobileInputManager.instance;
    }

    private static bool RaceActive =>
        LobbyState.Instance == null || LobbyState.Instance.IsRaceActive;

    public float Steer => _im != null && RaceActive ? _im.rotateHorizontal : 0f;
    public float Throttle => _im != null && RaceActive ? _im.rotateVertical : 0f;
    public bool JumpTapped => _im != null && RaceActive && _im.jumpTapped;

    public bool UseFront => _im != null && RaceActive && _im.weaponSelectAxis.y > 0f;
    public bool UseBack => _im != null && RaceActive && _im.weaponSelectAxis.y < 0f;
    public bool UseLeft => _im != null && RaceActive && _im.weaponSelectAxis.x < 0f;
    public bool UseRight => _im != null && RaceActive && _im.weaponSelectAxis.x > 0f;
}
