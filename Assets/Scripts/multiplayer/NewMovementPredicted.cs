using PurrNet.Prediction;
using UnityEngine;

public class NewMovementPredicted : PredictedIdentity<NewMovementPredicted.Input, NewMovementPredicted.State>
{
    [Header("Multiplayer")]
    [SerializeField] private PredictedRigidbody _rigidbody;
    [SerializeField] private GameObject visuals;

    [Header("MOVIMENTO")]
    [SerializeField] private float maxSpeed = 25f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float brakeDeceleration = 80f;
    [SerializeField] private float naturalDeceleration = 15f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField, Range(0f, 1f)] private float airSteerFactor = 0.3f;

    [Header("SALTO")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float gravity = 50f;
    [SerializeField] private float fastFallGravityMult = 2.5f;

    [Header("BOOST / DRIFT")]
    [SerializeField] private float boostImpulse = 15f;
    [SerializeField] private float driftChargeRate = 1.5f;
    [SerializeField] private float driftAngleThreshold = 0.3f;
    [SerializeField] private float driftReleaseThreshold = 0.15f;
    [SerializeField] private float sideGripFactor = 12f;

    [Header("ALLINEAMENTO TERRENO")]
    [SerializeField] private int raysCount = 8;
    [SerializeField] private float raySpread = 0.25f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRayLength = 0.6f;
    [SerializeField] private float groundCheckLength = 0.55f;
    [SerializeField] private float alignSpeed = 10f;
    [SerializeField] private float normalSmoothSpeed = 6f;

    private bool _cameraAssigned;
    private MobileInputManager _inputManager;

    public struct State : IPredictedData<State>
    {
        public bool grounded;
        public Vector3 smoothedNormal;
        public bool hasNormal;
        public float boostCharge;
        public float driftTimer;

        public void Dispose() { }
    }

    public struct Input : IPredictedData
    {
        public float steer;
        public float throttle;
        public bool jump;

        public void Dispose() { }
    }

    protected override void LateAwake()
    {
        _inputManager = MobileInputManager.instance;
        _rigidbody = GetComponent<PredictedRigidbody>();
        TryAssignLocalCamera();
    }

    protected override void Simulate(Input input, ref State state, float delta)
    {
        Rigidbody rb = _rigidbody.GetComponent<Rigidbody>();
        state.grounded = IsGrounded();

        // --- ALLINEAMENTO VISUALS AL TERRENO ---
        AlignToGround(ref state, delta);

        // --- STERZATA ---
        float steerAmount = input.steer * rotationSpeed;
        if (!state.grounded)
            steerAmount *= airSteerFactor;

        if (Mathf.Abs(input.steer) > 0.05f)
        {
            Vector3 upAxis = state.hasNormal && state.grounded ? state.smoothedNormal : Vector3.up;
            Quaternion steerRot = Quaternion.AngleAxis(steerAmount * delta, upAxis);
            _rigidbody.MoveRotation(steerRot * _rigidbody.rotation);
        }

        if (state.grounded)
        {
            rb.linearDamping = 0f;

            Vector3 slopeNormal = state.hasNormal ? state.smoothedNormal : Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(_rigidbody.transform.forward, slopeNormal).normalized;
            Vector3 right = Vector3.ProjectOnPlane(_rigidbody.transform.right, slopeNormal).normalized;

            Vector3 vel = _rigidbody.linearVelocity;
            Vector3 groundVel = Vector3.ProjectOnPlane(vel, slopeNormal);
            float verticalVel = Vector3.Dot(vel, slopeNormal);

            float forwardSpeed = Vector3.Dot(groundVel, forward);
            float sideSpeed = Vector3.Dot(groundVel, right);

            // --- ACCELERAZIONE / FRENATA ---
            if (input.throttle > 0.1f)
            {
                float targetSpeed = maxSpeed * input.throttle;
                float newSpeed = Mathf.MoveTowards(forwardSpeed, targetSpeed, acceleration * delta);
                forwardSpeed = newSpeed;
            }
            else if (input.throttle < -0.1f)
            {
                float brakeAmount = brakeDeceleration * Mathf.Abs(input.throttle) * delta;
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, brakeAmount);
            }
            else
            {
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, naturalDeceleration * delta);
            }

            // --- GRIP LATERALE (riduce scivolamento) ---
            sideSpeed = Mathf.MoveTowards(sideSpeed, 0f, sideGripFactor * delta);

            // --- DRIFT / BOOST ---
            float totalGroundSpeed = groundVel.magnitude;
            if (totalGroundSpeed > 1f)
            {
                float dirDot = Vector3.Dot(groundVel.normalized, forward);
                float misalignment = 1f - Mathf.Clamp01(dirDot);

                if (misalignment > driftAngleThreshold && totalGroundSpeed > 5f)
                {
                    state.driftTimer += delta;
                    if (state.driftTimer > 0.2f)
                        state.boostCharge = Mathf.Min(1f, state.boostCharge + driftChargeRate * delta);
                }
                else if (misalignment < driftReleaseThreshold && state.boostCharge > 0.1f)
                {
                    forwardSpeed += boostImpulse * state.boostCharge;
                    state.boostCharge = 0f;
                    state.driftTimer = 0f;
                }
                else if (misalignment <= driftAngleThreshold)
                {
                    state.driftTimer = 0f;
                }
            }

            // --- SLOPE PUSH (gravità proiettata sulla slope → spinta naturale in discesa) ---
            Vector3 slopeGravity = Vector3.ProjectOnPlane(Vector3.down * gravity, slopeNormal);
            float slopePush = Vector3.Dot(slopeGravity, forward);
            forwardSpeed += slopePush * delta;

            // --- APPLICA VELOCITÀ ---
            Vector3 newVel = forward * forwardSpeed + right * sideSpeed + slopeNormal * verticalVel;
            _rigidbody.linearVelocity = newVel;

            // --- SALTO ---
            if (input.jump)
            {
                Vector3 jumpVel = _rigidbody.linearVelocity;
                jumpVel += Vector3.up * jumpForce;
                jumpVel += forward * (jumpForce * 0.3f);
                _rigidbody.linearVelocity = jumpVel;
            }
        }
        else
        {
            // --- IN ARIA ---
            rb.linearDamping = 0f;

            // gravità custom (fast-fall se si scende)
            float gravMult = _rigidbody.linearVelocity.y < 0f ? fastFallGravityMult : 1f;
            Vector3 vel = _rigidbody.linearVelocity;
            vel.y -= gravity * gravMult * delta;
            _rigidbody.linearVelocity = vel;

            // leggero controllo direzionale in aria
            if (Mathf.Abs(input.throttle) > 0.1f)
            {
                Vector3 airForward = Vector3.ProjectOnPlane(_rigidbody.transform.forward, Vector3.up).normalized;
                Vector3 airVelH = Vector3.ProjectOnPlane(_rigidbody.linearVelocity, Vector3.up);
                float airSpeed = Vector3.Dot(airVelH, airForward);
                float airTarget = input.throttle > 0 ? maxSpeed * 0.5f : 0f;
                float newAirSpeed = Mathf.MoveTowards(airSpeed, airTarget, acceleration * 0.2f * delta);
                Vector3 airRight = Vector3.Cross(Vector3.up, airForward);
                float sideAir = Vector3.Dot(airVelH, airRight);
                Vector3 newVel = _rigidbody.linearVelocity;
                newVel.x = airForward.x * newAirSpeed + airRight.x * sideAir;
                newVel.z = airForward.z * newAirSpeed + airRight.z * sideAir;
                _rigidbody.linearVelocity = newVel;
            }

            state.boostCharge = Mathf.Max(0f, state.boostCharge - delta * 0.5f);
            state.driftTimer = 0f;
        }
    }

    private void AlignToGround(ref State state, float delta)
    {
        Vector3 origin = _rigidbody.position;
        Vector3 down = -_rigidbody.transform.up;
        float angleStep = 360f / raysCount;

        Vector3 normalSum = Vector3.zero;
        float weightSum = 0f;
        bool hit = false;

        for (int i = 0; i < raysCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 radial = _rigidbody.transform.right * Mathf.Cos(angle)
                           + _rigidbody.transform.forward * Mathf.Sin(angle);
            Vector3 dir = (down + radial * raySpread).normalized;

            if (Physics.Raycast(origin, dir, out RaycastHit hitInfo, groundRayLength, groundLayer))
            {
                hit = true;
                float t = hitInfo.distance / groundRayLength;
                float w = 1f - Mathf.Clamp01(t);
                normalSum += hitInfo.normal * w;
                weightSum += w;
                Debug.DrawRay(origin, dir * hitInfo.distance, Color.yellow);
            }
            else
            {
                Debug.DrawRay(origin, dir * groundRayLength, Color.red);
            }
        }

        if (!hit || weightSum < 0.0001f)
            return;

        Vector3 avgNormal = (normalSum / weightSum).normalized;

        if (!state.hasNormal)
        {
            state.smoothedNormal = avgNormal;
            state.hasNormal = true;
        }

        state.smoothedNormal = Vector3.Slerp(state.smoothedNormal, avgNormal, normalSmoothSpeed * delta);

        Vector3 projForward = Vector3.ProjectOnPlane(_rigidbody.transform.forward, state.smoothedNormal).normalized;
        if (projForward.sqrMagnitude < 0.0001f)
            projForward = Vector3.ProjectOnPlane(_rigidbody.transform.up, state.smoothedNormal).normalized;

        Quaternion target = Quaternion.LookRotation(projForward, state.smoothedNormal);
        Quaternion aligned = Quaternion.Slerp(_rigidbody.rotation, target, alignSpeed * delta);
        _rigidbody.MoveRotation(aligned);

        Debug.DrawRay(origin, state.smoothedNormal * 2f, Color.green);
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(_rigidbody.position, -_rigidbody.transform.up, out _, groundCheckLength, groundLayer);
    }

    private void TryAssignLocalCamera()
    {
        if (_cameraAssigned || !isOwner)
            return;

        if (PlayerCamera.instance == null)
            return;

        PlayerCamera.instance.SetTarget(visuals.transform);
        _cameraAssigned = true;
    }

    protected override void UpdateInput(ref Input input)
    {
        input.jump = _inputManager.jumpTapped;
    }

    protected override void GetFinalInput(ref Input input)
    {
        input.steer = _inputManager.rotateHorizontal;
        input.throttle = _inputManager.rotateVertical;
    }

    protected override void SanitizeInput(ref Input input)
    {
        input.steer = Mathf.Clamp(input.steer, -1f, 1f);
        input.throttle = Mathf.Clamp(input.throttle, -1f, 1f);
    }
}
