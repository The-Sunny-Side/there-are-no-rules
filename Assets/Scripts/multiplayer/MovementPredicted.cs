using PurrNet.Prediction;
using UnityEngine;

public class MovementPredicted : PredictedIdentity<MovementPredicted.Input, MovementPredicted.State>
{
    [Header("FISICA")]
    [SerializeField] private float gravityForce = 100f;
    [SerializeField] private float dragOnGround = 0.1f;
    [SerializeField] private float dragInAir = 0f;
    [SerializeField] private float normalAlignSpeed = 3f;
    [SerializeField] private float alignSpeed = 8f;
    [SerializeField] private PredictedRigidbody _rigidbody;


    [Header("CARATTERISTICHE")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float sideBreak = 20f;
    [SerializeField] private float forwardSpeed = 10f;
    [SerializeField] private float minAlignment = 0f;
    [SerializeField] private float boostForce = 100f;
    [SerializeField] private float speedToStartBoostDecay = 50f;
    [SerializeField] private float timeToStartBoostCharge = 0.3f;

    [Header("ALLINEAMENTO TERRENO")]
    [SerializeField] private int raysCount = 8;
    [SerializeField] private float raySpread = 0.25f;
    [SerializeField] private LayerMask whatIsGroud;
    [SerializeField] private float groundRayLength = .5f;
    [SerializeField] private float whenIsGroundLenght = .5f;

    private bool _cameraAssigned;
    private MobileInputManager _inputManager;
    public struct State : IPredictedData<State>
    {
        public void Dispose() { }
    }

    public struct Input : IPredictedData
    {
        public float turn;
        public bool jump;

        public void Dispose() { }
    }

    protected override void Simulate(Input input, ref State state, float delta)
    {
        if (Mathf.Abs(input.turn) > 0.0001f)
        {
            Quaternion d = Quaternion.Euler(0f, input.turn * rotationSpeed * delta, 0f);
            _rigidbody.MoveRotation(_rigidbody.rotation * d);
        }
    }

    protected override void LateAwake()
    {
        _inputManager = MobileInputManager.instance;

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

    protected override void UpdateInput(ref Input input)
    {
        input.jump = _inputManager.jumpTapped;
       
    }
    protected override void GetFinalInput(ref Input input)
    {
        
        float turn = 0f;
        if (_inputManager.rightHeld) turn += 1f;
        if (_inputManager.leftHeld) turn -= 1f;
        input.turn = turn;

    }

    protected override void SanitizeInput(ref Input input)
    {
        input.turn = Mathf.Clamp(input.turn, -1f, 1f);

    }
}
