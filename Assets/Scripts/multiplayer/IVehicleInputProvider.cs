public interface IVehicleInputProvider
{
    float Steer { get; }
    float Throttle { get; }
    bool JumpTapped { get; }

    bool UseFront { get; }
    bool UseBack { get; }
    bool UseLeft { get; }
    bool UseRight { get; }
}
