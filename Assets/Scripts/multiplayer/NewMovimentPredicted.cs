using PurrNet.Prediction;
using UnityEngine;

[RequireComponent(typeof(PredictedRigidbody))]
public class NewMovimentPredicted : PredictedIdentity<NewMovimentPredicted.Input, NewMovimentPredicted.State>
{
    [Header("REFERENCES")]
    [SerializeField] private PredictedRigidbody _rigidbody;

    [Header("FISICA")]
    [SerializeField] private float gravityForce = 100f;
    [SerializeField] private float dragOnGround = 0.1f;
    [SerializeField] private float dragInAir = 0f;
    [SerializeField] private float normalAlignSpeed = 3f;
    [SerializeField] private float alignSpeed = 8f;

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
    [SerializeField] private float debugRayDuration = 0.05f;

    [Header("DEBUG (READ ONLY DURING PLAY)")]
    [SerializeField] private float boostAccumulato = 0f;
    [SerializeField] private bool grounded;
    [SerializeField] private float driftingTime = 0f;

    private MobileInputManager _inputManager;
    private bool _cameraAssigned;
    private int _resolvedGroundMask;
    private bool _groundMaskResolved;

    private void Reset()
    {
        if (!TryGetComponent(out _rigidbody))
            _rigidbody = gameObject.AddComponent<PredictedRigidbody>();
    }

    private void OnValidate()
    {
        _groundMaskResolved = false;
    }

    protected override State GetInitialState()
    {
        return new State
        {
            smoothedNormal = Vector3.up
        };
    }

    protected override void LateAwake()
    {
        EnsurePredictedRigidbody();
        ResolveGroundMask();
        EnsureInputManager();
        TryAssignLocalCamera();
    }

    public override void OnViewOwnerChanged(PurrNet.PlayerID? oldOwner, PurrNet.PlayerID? newOwner)
    {
        _cameraAssigned = false;
        TryAssignLocalCamera();
    }

    protected override void UpdateInput(ref Input input)
    {
        EnsureInputManager();
        TryAssignLocalCamera();

        if (_inputManager == null)
            return;

        if (_inputManager.jumpTapped)
            input.jumpPressed = true;
    }

    protected override void GetFinalInput(ref Input input)
    {
        EnsureInputManager();

        if (_inputManager == null)
        {
            input.turnInput = 0f;
            return;
        }

        float turn = 0f;
        if (_inputManager.rightHeld) turn += 1f;
        if (_inputManager.leftHeld) turn -= 1f;

        input.turnInput = Mathf.Clamp(turn, -1f, 1f);
    }

    protected override void Simulate(Input input, ref State state, float delta)
    {
        if (!EnsurePredictedRigidbody())
            return;

        state.grounded = IsGrounded();
        bool alignedToGround = CastGroundRaysAndAlign(ref state, delta, input.turnInput);
        if (!alignedToGround && Mathf.Abs(input.turnInput) > 0.0001f)
        {
            Quaternion deltaRotation = Quaternion.Euler(0f, input.turnInput * rotationSpeed * delta, 0f);
            _rigidbody.MoveRotation(_rigidbody.rotation * deltaRotation);
        }

        if (state.grounded && input.jumpPressed)
        {
            _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _rigidbody.AddForce(_rigidbody.transform.forward * jumpForce, ForceMode.Impulse);
        }

        if (state.grounded)
        {
            _rigidbody.rigidbody.linearDamping = dragOnGround;

            if (Physics.Raycast(_rigidbody.position, -Vector3.up, out RaycastHit hit, whenIsGroundLenght, GetGroundMask()))
            {
                Vector3 slopeNormal = hit.normal;
                Vector3 slopeDirection = Vector3.Cross(slopeNormal, Vector3.Cross(Vector3.down, slopeNormal)).normalized;

                Vector3 forwardOnSlope = Vector3.ProjectOnPlane(_rigidbody.transform.forward, slopeNormal).normalized;

                float alignment = Mathf.Clamp01(Vector3.Dot(forwardOnSlope, slopeDirection));
                alignment = Mathf.Max(alignment, minAlignment);

                Vector3 slideForce = forwardOnSlope * gravityForce * alignment * forwardSpeed;
                Debug.DrawRay(_rigidbody.position, slideForce * 5f, Color.cyan);

                _rigidbody.AddForce(slideForce);

                if (_rigidbody.linearVelocity.sqrMagnitude > 0.1f)
                {
                    Vector3 velDir = _rigidbody.linearVelocity.normalized;
                    float dirAlignment = Vector3.Dot(velDir, forwardOnSlope);
                    float misalignment = 1f - Mathf.Max(0f, dirAlignment);

                    if (misalignment < 0.3f)
                    {
                        _rigidbody.AddForce(slideForce * state.boostAccumulato * boostForce, ForceMode.Impulse);
                        state.boostAccumulato = 0f;
                        state.driftingTime = 0f;
                    }
                    else if (_rigidbody.linearVelocity.sqrMagnitude < speedToStartBoostDecay)
                    {
                        state.boostAccumulato = Mathf.Max(0f, state.boostAccumulato - delta);
                    }
                    else if (state.driftingTime > timeToStartBoostCharge)
                    {
                        state.boostAccumulato = Mathf.Min(1f, state.boostAccumulato + misalignment * delta);
                    }
                    else
                    {
                        state.driftingTime += delta;
                    }

                    _rigidbody.linearVelocity *= Mathf.Lerp(1f, 0.9f, misalignment * delta * sideBreak);
                }
            }
        }
        else
        {
            _rigidbody.rigidbody.linearDamping = dragInAir;
            _rigidbody.AddForce(Vector3.down * gravityForce);
        }

        boostAccumulato = state.boostAccumulato;
        grounded = state.grounded;
        driftingTime = state.driftingTime;
    }

    private bool CastGroundRaysAndAlign(ref State state, float delta, float turnInput)
    {
        Vector3 origin = _rigidbody.position;
        Vector3 down = -_rigidbody.transform.up;

        int safeRaysCount = Mathf.Max(1, raysCount);
        float angleStep = 360f / safeRaysCount;

        Vector3 weightedNormalSum = Vector3.zero;
        float weightSum = 0f;
        bool foundHit = false;

        for (int i = 0; i < safeRaysCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 radial =
                _rigidbody.transform.right * Mathf.Cos(angle) +
                _rigidbody.transform.forward * Mathf.Sin(angle);

            Vector3 dir = (down + radial * raySpread).normalized;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, groundRayLength, GetGroundMask()))
            {
                foundHit = true;
                Debug.DrawRay(origin, dir * hit.distance, Color.yellow, debugRayDuration);

                float t = hit.distance / groundRayLength;
                float weight = 1f - Mathf.Clamp01(t);

                weightedNormalSum += hit.normal * weight;
                weightSum += weight;
            }
            else
            {
                Debug.DrawRay(origin, dir * groundRayLength, Color.red, debugRayDuration);
            }
        }

        if (!foundHit || weightSum <= 0.0001f)
            return false;

        Vector3 averagedNormal = (weightedNormalSum / weightSum).normalized;

        if (!state.hasSmoothedNormal)
        {
            state.smoothedNormal = averagedNormal;
            state.hasSmoothedNormal = true;
        }

        state.smoothedNormal = Vector3.Slerp(
            state.smoothedNormal,
            averagedNormal,
            normalAlignSpeed * delta
        );

        float yawDelta = turnInput * rotationSpeed * delta;
        Vector3 yawedForward = Quaternion.Euler(0f, yawDelta, 0f) * _rigidbody.transform.forward;
        Vector3 forwardProjected = Vector3.ProjectOnPlane(yawedForward, state.smoothedNormal).normalized;

        if (forwardProjected.sqrMagnitude < 0.0001f)
            forwardProjected = Vector3.ProjectOnPlane(_rigidbody.transform.up, state.smoothedNormal).normalized;

        if (forwardProjected.sqrMagnitude < 0.0001f)
            return false;

        Quaternion targetRotation = Quaternion.LookRotation(forwardProjected, state.smoothedNormal);
        Quaternion newRotation = Quaternion.Slerp(_rigidbody.rotation, targetRotation, alignSpeed * delta);

        _rigidbody.MoveRotation(newRotation);
        Debug.DrawRay(origin, state.smoothedNormal * 100f, Color.green, debugRayDuration);
        return true;
    }

    private bool IsGrounded()
    {
        Vector3 origin = _rigidbody.position;
        Vector3 direction = -_rigidbody.transform.up;
        bool hit = Physics.Raycast(origin, direction, out _, whenIsGroundLenght, GetGroundMask());
        Debug.DrawRay(origin, direction * whenIsGroundLenght, hit ? Color.green : Color.magenta, debugRayDuration);
        return hit;
    }

    private void EnsureInputManager()
    {
        if (_inputManager == null)
            _inputManager = MobileInputManager.instance;
    }

    private bool EnsurePredictedRigidbody()
    {
        if (_rigidbody != null)
            return true;

        TryGetComponent(out _rigidbody);
        return _rigidbody != null;
    }

    private int GetGroundMask()
    {
        if (!_groundMaskResolved)
            ResolveGroundMask();
        return _resolvedGroundMask;
    }

    private void ResolveGroundMask()
    {
        if (whatIsGroud.value != 0)
        {
            _resolvedGroundMask = whatIsGroud.value;
            _groundMaskResolved = true;
            return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
        {
            _resolvedGroundMask = 1 << groundLayer;
            _groundMaskResolved = true;
            Debug.LogWarning($"[{nameof(NewMovimentPredicted)}] whatIsGroud e' vuoto. Uso fallback layer 'Ground'.", this);
            return;
        }

        _resolvedGroundMask = Physics.DefaultRaycastLayers;
        _groundMaskResolved = true;
        Debug.LogWarning($"[{nameof(NewMovimentPredicted)}] whatIsGroud e layer 'Ground' non trovati. Uso DefaultRaycastLayers.", this);
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

    public struct State : IPredictedData<State>
    {
        public float boostAccumulato;
        public bool grounded;
        public float driftingTime;
        public Vector3 smoothedNormal;
        public bool hasSmoothedNormal;

        public void Dispose() { }
    }

    public struct Input : IPredictedData
    {
        public float turnInput;
        public bool jumpPressed;

        public void Dispose() { }
    }
}
