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
        public float boostAccumulato;
        public float driftingTime;
        public bool grounded;
        public Vector3 smoothedNormal;
        public bool hasSmoothedNormal;



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
        CastGroundRaysAndAlign(ref state, delta);

        if (Mathf.Abs(input.turn) > 0.0001f)
        {
            Quaternion d = Quaternion.Euler(0f, input.turn * rotationSpeed * delta, 0f);
            _rigidbody.MoveRotation(_rigidbody.rotation * d);
        }

        state.grounded = IsGrounded();
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
        }
        else
        {
            _rigidbody.GetComponent<Rigidbody>().linearDamping = dragInAir;
            _rigidbody.AddForce(Vector3.down * gravityForce);
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

        PlayerCamera.instance.SetTarget(visuals.transform);
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
    private bool IsGrounded()
    {
        return Physics.Raycast(_rigidbody.position, -_rigidbody.transform.up, out _, whenIsGroundLenght, whatIsGroud);
    }

    private void CastGroundRaysAndAlign(ref State state, float delta)
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
