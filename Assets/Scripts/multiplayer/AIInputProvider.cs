using UnityEngine;

public class AIInputProvider : MonoBehaviour, IVehicleInputProvider
{
    public float Steer { get; set; }
    public float Throttle { get; set; }
    public bool JumpTapped { get; set; }

    public bool UseFront { get; set; }
    public bool UseBack { get; set; }
    public bool UseLeft { get; set; }
    public bool UseRight { get; set; }
}
