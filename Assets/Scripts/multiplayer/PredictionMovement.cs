using PurrNet.Prediction;
using UnityEngine;

public class PredictionMovement : PredictedIdentity<PredictionMovement.Input, PredictionMovement.State>
{
    [SerializeField] private PredictedRigidbody _rigidbody;
    [SerializeField] private float _forwardImpulse = 8f;
    [SerializeField] private float _rotationSpeed = 220f;
    private MobileInputManager _inputManager;
    private bool _cameraAssigned;

    protected override void Simulate(Input input, ref State state, float delta)
    {
        if (_rigidbody == null)
            return;

        if (Mathf.Abs(input.turnInput) > 0.0001f)
        {
            Quaternion deltaRotation = Quaternion.Euler(0f, input.turnInput * _rotationSpeed * delta, 0f);
            _rigidbody.MoveRotation(_rigidbody.rotation * deltaRotation);
        }

        if (input.forwardPressed)
            _rigidbody.AddForce(_rigidbody.transform.forward * _forwardImpulse, ForceMode.Impulse);
    }

    protected override void GetFinalInput(ref Input input)
    {
        if (_inputManager == null)
            _inputManager = MobileInputManager.instance;

        if (_inputManager == null)
            return;

        float turn = 0f;
        if (_inputManager.rightHeld) turn += 1f;
        if (_inputManager.leftHeld) turn -= 1f;

        input.turnInput = Mathf.Clamp(turn, -1f, 1f);
        input.forwardPressed = _inputManager.jumpTapped;
    }

    public struct State : IPredictedData<State>
    {
        public void Dispose() { }
    }

    public struct Input : IPredictedData
    {
        public float turnInput;
        public bool forwardPressed;

        public void Dispose() { }
    }
    protected override void LateAwake()
    {
        
        TryAssignLocalCamera();
    }
    private void TryAssignLocalCamera()
    {
        if (_cameraAssigned || !isOwner)
            return;

        if (PlayerCamera.instance == null)
            return;

        PlayerCamera.instance.SetTarget(transform);
        _cameraAssigned = true;
    }
}
