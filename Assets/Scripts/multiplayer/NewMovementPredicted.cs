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
    [SerializeField] private float groundRotationSpeed = 180f;
    [SerializeField] private float airRotationSpeed = 60f;

    [Header("SALTO")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float gravity = 50f;
    [SerializeField] private float fastFallGravityMult = 2.5f;

    [Header("GRAVITÀ SLOPE")]
    [Tooltip("Accelerazione aggiuntiva lungo la pendenza in discesa (unità/s²)")]
    [SerializeField] private float slopeAcceleration = 50f;

    [Header("BOOST / DRIFT")]
    [Tooltip("Impulso in avanti applicato quando un drift caricato viene rilasciato. Viene moltiplicato per la carica accumulata.")]
    [SerializeField] private float boostImpulse = 15f;
    [Tooltip("Durata (secondi) per cui il VFX di boost resta acceso dopo il rilascio del drift.")]
    [SerializeField] private float boostDuration = 0.6f;
    [Tooltip("Velocità con cui si carica il boost mentre la sfera sta driftando dopo il breve ritardo iniziale.")]
    [SerializeField] private float driftChargeRate = 1.5f;
    [Tooltip("Soglia di disallineamento tra velocità e direzione frontale oltre la quale inizia il drift. Valori più bassi lo attivano prima.")]
    [SerializeField] private float driftAngleThreshold = 0.3f;
    [Tooltip("Soglia di riallineamento sotto la quale viene rilasciato il boost accumulato dal drift.")]
    [SerializeField] private float driftReleaseThreshold = 0.15f;
    [Tooltip("Velocita minima sul terreno richiesta per mantenere o accumulare carica drift.")]
    [SerializeField] private float driftMinSpeed = 5f;
    [Tooltip("Forza con cui viene ridotta la velocità laterale sul terreno. Valori più alti danno più grip e meno scivolamento.")]
    [SerializeField] private float sideGripFactor = 12f;

    [Header("ALLINEAMENTO TERRENO")]
    [SerializeField] private int raysCount = 8;
    [SerializeField] private float raySpread = 0.25f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRayLength = 0.6f;
    [SerializeField] private float groundCheckLength = 0.55f;
    [SerializeField] private float alignSpeed = 10f;
    [SerializeField] private float normalSmoothSpeed = 6f;

    [Header("CAMERA")]
    [Tooltip("Secondi in aria prima di congelare lo yaw della camera")]
    [SerializeField] private float airCameraDelay = 0.5f;

    private CameraPivot _cameraPivot;
    private bool _cameraAssigned;
    private bool _lastPivotGrounded = true;
    private float _airTime;

    private VFXGroup _driftVFX;
    private VFXGroup _boostVFX;

    private IVehicleInputProvider _input;

    public struct State : IPredictedData<State>
    {
        public bool grounded;
        public Vector3 smoothedNormal;
        public bool hasNormal;
        public float boostCharge;
        public float driftTimer;
        public float boostTimer;

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
        _input = GetComponent<IVehicleInputProvider>();
        _rigidbody = GetComponent<PredictedRigidbody>();
        TryAssignLocalCamera();
        var loader = GetComponentInChildren<VehicleLoader>();

        if (loader != null)
            loader.OnVehicleBuilt += CacheVehicleVFX;
    }

    private void CacheVehicleVFX()
    {
        _driftVFX = VFXGroup.FromChildren(gameObject, VFXType.Drift);
        _boostVFX = VFXGroup.FromChildren(gameObject, VFXType.Boost);
    }

    protected override void Simulate(Input input, ref State state, float delta)
    {
        Rigidbody rb = _rigidbody.GetComponent<Rigidbody>();
        state.grounded = IsGrounded();

        // --- ALLINEAMENTO VISUALS AL TERRENO ---
        AlignToGround(ref state, delta);

        // --- BOOST VFX (acceso per tutta la durata del boost) ---
        if (state.boostTimer > 0f)
        {
            state.boostTimer -= delta;
            _boostVFX.Play();
            if (state.boostTimer <= 0f)
            {
                state.boostTimer = 0f;
                _boostVFX.Stop();
            }
        }

        // --- STERZATA ---
        if (state.grounded)
        {
            if (Mathf.Abs(input.steer) > 0.05f)
            {
                Vector3 upAxis = state.hasNormal ? state.smoothedNormal : Vector3.up;
                Quaternion steerRot = Quaternion.AngleAxis(input.steer * groundRotationSpeed * delta, upAxis);
                _rigidbody.MoveRotation(steerRot * _rigidbody.rotation);
            }
        }
        else
        {
            Vector3 axis = _rigidbody.transform.up * input.steer
                         + _rigidbody.transform.right * input.throttle;
            float magnitude = axis.magnitude;
            if (magnitude > 0.05f)
            {
                float angle = magnitude * airRotationSpeed * delta;
                _rigidbody.MoveRotation(Quaternion.AngleAxis(angle, axis / magnitude) * _rigidbody.rotation);
            }
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
            if (totalGroundSpeed < driftMinSpeed || totalGroundSpeed <= 1f)
            {
                state.boostCharge = 0f;
                state.driftTimer = 0f;
                _driftVFX.Stop();
            }
            else
            {
                float dirDot = Vector3.Dot(groundVel.normalized, forward);
                float misalignment = 1f - Mathf.Clamp01(dirDot);
                if (misalignment > driftAngleThreshold)
                {
                    _driftVFX.Play();

                    state.driftTimer += delta;
                    if (state.driftTimer > 0.2f)
                        state.boostCharge = Mathf.Min(1f, state.boostCharge + driftChargeRate * delta);
                }
                else if (misalignment < driftReleaseThreshold && state.boostCharge > 0.1f)
                {
                    _driftVFX.Stop();

                    // NOTA: il boost di gameplay è un impulso istantaneo (la velocità extra
                    // decade subito con l'attrito). boostTimer serve solo a tenere acceso il VFX
                    // per boostDuration secondi. Se in futuro vuoi una SPINTA sostenuta e non solo
                    // l'effetto visivo, usa lo stesso boostTimer nel blocco grounded, es:
                    //   if (state.boostTimer > 0f) forwardSpeed = Mathf.Max(forwardSpeed, maxSpeed * 1.15f);
                    forwardSpeed += boostImpulse * state.boostCharge;
                    state.boostTimer = boostDuration;
                    state.boostCharge = 0f;
                    state.driftTimer = 0f;
                }
                else if (misalignment <= driftAngleThreshold)
                {
                    _driftVFX.Stop();
                    state.driftTimer = 0f;
                }
            }

            // --- SLOPE PUSH (accelerazione aggiuntiva lungo la pendenza in discesa) ---
            Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, slopeNormal);
            float slopePush = Vector3.Dot(slopeDir, forward) * slopeAcceleration;
            forwardSpeed += slopePush * delta;
            forwardSpeed = Mathf.Clamp(forwardSpeed, -maxSpeed, maxSpeed * 1.3f); // niente accumulo infinito in discesa

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
            _driftVFX.Stop();
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
    void LateUpdate()
    {
        if (!isOwner) return;

        TryAssignLocalCamera();

        bool grounded = IsGrounded();

        if (grounded)
        {
            _airTime = 0f;
            if (!_lastPivotGrounded)
            {
                _lastPivotGrounded = true;
                _cameraPivot?.SetGrounded(true);
            }
        }
        else
        {
            _airTime += Time.deltaTime;
            if (_lastPivotGrounded && _airTime >= airCameraDelay)
            {
                _lastPivotGrounded = false;
                _cameraPivot?.SetGrounded(false);
            }
        }
    }

    private void TryAssignLocalCamera()
    {
        if (_cameraAssigned || !isOwner)
            return;

        if (PlayerCamera.instance == null)
            return;

        var pivotGo = new GameObject("CameraPivot");
        pivotGo.transform.SetParent(transform, worldPositionStays: false);
        _cameraPivot = pivotGo.AddComponent<CameraPivot>();
        _cameraPivot.Init(visuals.transform);

        PlayerCamera.instance.SetTarget(_cameraPivot.transform);
        _cameraAssigned = true;
    }

    protected override void UpdateInput(ref Input input)
    {
        input.jump = _input.JumpTapped;
    }

    protected override void GetFinalInput(ref Input input)
    {
        input.steer = _input.Steer;
        input.throttle = _input.Throttle;
    }

    protected override void SanitizeInput(ref Input input)
    {
        input.steer = Mathf.Clamp(input.steer, -1f, 1f);
        input.throttle = Mathf.Clamp(input.throttle, -1f, 1f);
    }
}
