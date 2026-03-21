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

    [Header("Multiplayer")]
    [SerializeField] private PredictedRigidbody _rigidbody;
    [SerializeField] private GameObject visuals;

    [Header("CARATTERISTICHE")]
    [Tooltip("Velocità di rotazione sinistra/destra (gradi/sec)")]
    [SerializeField] private float rotationSpeed = 90f;
    [Tooltip("Moltiplicatore della rotazione in aria (0 = nessuna sterzata, 1 = sterzata piena)")]
    [SerializeField, Range(0f, 1f)] private float airRotationMultiplier = 0.35f;
    [Tooltip("Forza applicata verso l'alto al salto")]
    [SerializeField] private float jumpForce = 10f;
    [Tooltip("Forza di frenata laterale per ridurre lo scivolamento di lato")]
    [SerializeField] private float sideBreak = 20f;
    [Tooltip("Velocità massima in avanti a terra")]
    [SerializeField] private float forwardSpeed = 10f;
    [Tooltip("Tasso di accelerazione a terra: più alto = risposta più immediata al gas")]
    [SerializeField] private float groundedAcceleration = 30f;
    [Tooltip("Forza di frenata quando si spinge l'analogico indietro a terra")]
    [SerializeField] private float groundedDeceleration = 45f;
    [Tooltip("Soglia minima dell'input sotto cui non si applica accelerazione (evita micro-input accidentali)")]
    [SerializeField, Range(0f, 1f)] private float groundedDriveDeadzone = 0.15f;
    [Tooltip("Soglia minima dell'input freno sotto cui non si applica decelerazione forzata")]
    [SerializeField, Range(0f, 1f)] private float groundedBrakeDeadzone = 0.2f;
    [Tooltip("Soglia minima dell'input sterzata sotto cui non si ruota (elimina il drift dello stick)")]
    [SerializeField, Range(0f, 1f)] private float groundedSteerDeadzone = 0.2f;
    [Tooltip("Allineamento minimo con la slope")]
    [SerializeField] private float minAlignment = 0f;
    [Tooltip("Forza del boost applicata al rilascio")]
    [SerializeField] private float boostForce = 100f;
    [Tooltip("Velocità oltre cui il boost inizia a decadere (non si accumula più boost sopra questa soglia)")]
    [SerializeField] private float speedToStartBoostDecay = 50f;
    [Tooltip("Secondi di pressione continua del freno necessari prima di iniziare a caricare il boost")]
    [SerializeField] private float timeToStartBoostCharge = 0.3f;

    [Header("ALLINEAMENTO TERRENO")]
    [Tooltip("Numero di raggi usati per rilevare la normale del terreno (più raggi = più preciso ma più costoso)")]
    [SerializeField] private int raysCount = 8;
    [Tooltip("Raggio della distribuzione dei raggi attorno al centro del personaggio")]
    [SerializeField] private float raySpread = 0.25f;
    [Tooltip("Layer che viene considerato come terreno")]
    [SerializeField] private LayerMask whatIsGroud;
    [Tooltip("Lunghezza dei raggi usati per calcolare la normale del terreno")]
    [SerializeField] private float groundRayLength = .5f;
    [Tooltip("Lunghezza del raggio usato per determinare se il personaggio è a terra")]
    [SerializeField] private float whenIsGroundLenght = .5f;

    private bool _cameraAssigned;
    private MobileInputManager _inputManager;
    protected override void LateAwake()
    {
        _inputManager = MobileInputManager.instance;
        _rigidbody = GetComponent<PredictedRigidbody>();

        TryAssignLocalCamera();
    }
    public struct State : IPredictedData<State>
    {
        public float boostAccumulato;
        public float driftingTime;
        public bool grounded;
        public Vector3 smoothedNormal;
        public bool hasSmoothedNormal;



        public void Dispose() { }
    }

    public struct Input : IPredictedData
    {
        public float horizontalTurn;
        public float verticalTurn;
        public bool jump;

        public void Dispose() { }
    }

    protected override void Simulate(Input input, ref State state, float delta)
    {
        state.grounded = IsGrounded();
        Vector2 moveInput = new Vector2(input.horizontalTurn, input.verticalTurn);
        float airRotationSpeed = rotationSpeed * airRotationMultiplier;

        CastGroundRaysAndAlign(ref state, moveInput, delta);

        if (!state.grounded && Mathf.Abs(input.horizontalTurn) > 0.0001f)
        {
            Vector3 axis = state.hasSmoothedNormal ? state.smoothedNormal : _rigidbody.transform.up;
            _rigidbody.AddTorque(axis * (input.horizontalTurn * airRotationSpeed), ForceMode.Acceleration);
        }
        if (!state.grounded && Mathf.Abs(input.verticalTurn) > 0.0001f)
        {
            Vector3 axis = _rigidbody.transform.right;
            _rigidbody.AddTorque(axis * (input.verticalTurn * airRotationSpeed), ForceMode.Acceleration);
        }

        if (state.grounded && input.jump)
        {
            _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _rigidbody.AddForce(_rigidbody.transform.forward * jumpForce, ForceMode.Impulse);
        }
        if (state.grounded)
        {
            _rigidbody.GetComponent<Rigidbody>().linearDamping = dragOnGround;

            if (Physics.Raycast(_rigidbody.position, -Vector3.up, out RaycastHit hit, whenIsGroundLenght, whatIsGroud))
            {
                Vector3 slopeNormal = hit.normal;
                Vector3 slopeDirection = Vector3.Cross(slopeNormal, Vector3.Cross(Vector3.down, slopeNormal)).normalized;

                // forward proiettato sulla pendenza
                Vector3 forwardOnSlope = Vector3.ProjectOnPlane(_rigidbody.transform.forward, slopeNormal).normalized;
                float rawInputMagnitude = Mathf.Clamp01(moveInput.magnitude);
                bool hasDriveInput = rawInputMagnitude >= groundedDriveDeadzone;
                bool isBrakeInput = moveInput.y < -groundedBrakeDeadzone;

                if (isBrakeInput)
                {
                    Vector3 groundVelocity = Vector3.ProjectOnPlane(_rigidbody.linearVelocity, slopeNormal);
                    if (groundVelocity.sqrMagnitude > 0.0001f)
                        _rigidbody.AddForce(-groundVelocity.normalized * groundedDeceleration, ForceMode.Acceleration);

                    state.driftingTime = 0f;
                }
                else if (hasDriveInput)
                {
                    float driveInputMagnitude = Mathf.InverseLerp(groundedDriveDeadzone, 1f, rawInputMagnitude);
                    _rigidbody.AddForce(forwardOnSlope * (groundedAcceleration * driveInputMagnitude), ForceMode.Acceleration);

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

                        // boost release
                        if (misalignment < 0.3f)
                        {
                            _rigidbody.AddForce(slideForce * state.boostAccumulato * boostForce, ForceMode.Impulse);
                            state.boostAccumulato = 0f;
                            state.driftingTime = 0f;
                        }
                        // boost decay
                        else if (_rigidbody.linearVelocity.sqrMagnitude < speedToStartBoostDecay)
                        {
                            state.boostAccumulato = Mathf.Max(0f, state.boostAccumulato - delta);
                        }
                        // boost charge
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
                else
                {
                    state.driftingTime = 0f;
                }
            }
        }
        else
        {
            _rigidbody.GetComponent<Rigidbody>().linearDamping = dragInAir;
            _rigidbody.AddForce(Vector3.down * gravityForce);
        }
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
        float horizontalTurn = _inputManager.rotateHorizontal;
        input.horizontalTurn = horizontalTurn;

        float verticalTurn = _inputManager.rotateVertical;
        input.verticalTurn = verticalTurn;

    }

    protected override void SanitizeInput(ref Input input)
    {
        input.horizontalTurn = Mathf.Clamp(input.horizontalTurn, -1f, 1f);
        input.verticalTurn = Mathf.Clamp(input.verticalTurn, -1f, 1f);


    }
    private bool IsGrounded()
    {
        return Physics.Raycast(_rigidbody.position, -_rigidbody.transform.up, out _, whenIsGroundLenght, whatIsGroud);
    }

    private void CastGroundRaysAndAlign(ref State state, Vector2 moveInput, float delta)
    {
        Vector3 origin = _rigidbody.position;
        Vector3 down = -_rigidbody.transform.up;

        float angleStep = 360f / raysCount;

        // accumulo per la media ponderata
        Vector3 weightedNormalSum = Vector3.zero;
        float weightSum = 0f;
        bool foundHit = false;

        for (int i = 0; i < raysCount; i++)
        {
            // angolo sul piano XZ (locale)
            float angle = angleStep * i * Mathf.Deg2Rad;

            // direzione �laterale� intorno all�oggetto (sul piano orizzontale)
            Vector3 radial =
                _rigidbody.transform.right * Mathf.Cos(angle) +
                _rigidbody.transform.forward * Mathf.Sin(angle);

            // direzione finale del raggio: verso il basso ma leggermente inclinata
            Vector3 dir = (down + radial * raySpread).normalized;

            RaycastHit hit;
            if (Physics.Raycast(origin, dir, out hit, groundRayLength, whatIsGroud))
            {
                foundHit = true;

                // disegna il raggio che colpisce in giallo
                Debug.DrawRay(origin, dir * hit.distance, Color.yellow);

                // ---- PESO IN BASE ALLA VICINANZA ----
                float t = hit.distance / groundRayLength; // 0 vicino, 1 al limite
                float weight = 1f - Mathf.Clamp01(t);     // 1 vicino, 0 lontano

                weightedNormalSum += hit.normal * weight;
                weightSum += weight;
            }
            else
            {
                // disegna il raggio che NON colpisce in rosso
                Debug.DrawRay(origin, dir * groundRayLength, Color.red);
            }
        }

        // dopo aver controllato tutti i raggi, calcoliamo la normale mediata
        if (foundHit && weightSum > 0.0001f)
        {
            Vector3 averagedNormal = (weightedNormalSum / weightSum).normalized;

            // inizializza la smoothedNormal al primo frame utile
            if (!state.hasSmoothedNormal)
            {
                state.smoothedNormal = averagedNormal;
                state.hasSmoothedNormal = true;
            }

            // interpolazione morbida della normale
            state.smoothedNormal = Vector3.Slerp(
                state.smoothedNormal,
                averagedNormal,
                normalAlignSpeed * delta
            );

            // proietta forward sul piano definito dalla NORMALE SMUSSATA
            Vector3 forwardProjected = Vector3.ProjectOnPlane(_rigidbody.transform.forward, state.smoothedNormal).normalized;

            // fallback nel caso raro in cui forwardProjected diventi quasi zero
            if (forwardProjected.sqrMagnitude < 0.0001f)
                forwardProjected = Vector3.ProjectOnPlane(_rigidbody.transform.up, state.smoothedNormal).normalized;

            if (state.grounded && moveInput.sqrMagnitude > 0.0001f)
            {
                float steerStrength = Mathf.InverseLerp(groundedSteerDeadzone, 1f, Mathf.Abs(moveInput.x));

                Vector3 desiredDirection =
                    _rigidbody.transform.right * (Mathf.Sign(moveInput.x) * steerStrength) +
                    _rigidbody.transform.forward * moveInput.y;

                desiredDirection = Vector3.ProjectOnPlane(desiredDirection, state.smoothedNormal).normalized;

                if (desiredDirection.sqrMagnitude > 0.0001f && steerStrength > 0f)
                {
                    forwardProjected = Vector3.RotateTowards(
                        forwardProjected,
                        desiredDirection,
                        rotationSpeed * steerStrength * Mathf.Deg2Rad * delta,
                        0f);
                }
            }

            Quaternion targetRotation = Quaternion.LookRotation(forwardProjected, state.smoothedNormal);

            // IMPORTANT: niente transform.rotation -> usa MoveRotation
            Quaternion newRot = Quaternion.Slerp(
                _rigidbody.rotation,
                targetRotation,
                alignSpeed * delta
            );

            _rigidbody.MoveRotation(newRot);

            // raggio verde: usa la normale smussata
            Debug.DrawRay(origin, state.smoothedNormal * 100f, Color.green);
        }
    }
}
