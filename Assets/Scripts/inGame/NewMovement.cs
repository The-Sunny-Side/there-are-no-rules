using PurrNet;
using UnityEngine;

public class NewMovement : NetworkIdentity
{
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

    public float boostAccumulato = 0f;
    public bool grounded;
    public float driftingTime = 0f;

    private Vector3 smoothedNormal = Vector3.up;
    private bool hasSmoothedNormal = false;

    private MobileInputManager inputManager;
    private Rigidbody _rb;

    // input cache (letto in Update, usato in FixedUpdate)
    private float _turnInput;
    private float _forwardInput;
    private bool _jumpPressed;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        _rb = GetComponent<Rigidbody>();

        // Solo l'owner simula
        enabled = isOwner;

        if (isOwner)
        {
            // smoothing fisico locale (aiuta tanto)
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.isKinematic = false;
            _rb.useGravity = true;
        }
        else
        {
            // opzionale ma consigliato (evita divergenze lato remoto)
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
    }

    void Start()
    {
        inputManager = MobileInputManager.instance;

        if (isOwner)
            PlayerCamera.instance.SetTarget(transform);
    }

    void Update()
    {
        // solo owner (lo script � gi� disabled per non-owner, ma meglio safe)
        if (!isOwner || inputManager == null) return;

        // Leggo input qui, ma NON muovo fisica/transform qui.
        _turnInput = 0f;
        if (inputManager.rightHeld) _turnInput += 1f;
        if (inputManager.leftHeld) _turnInput -= 1f;

        _forwardInput = inputManager.rotateVertical;

        // edge-trigger: salvo la pressione salto per FixedUpdate
        if (inputManager.jumpTapped)
            _jumpPressed = true;
    }

    void FixedUpdate()
    {
        if (!isOwner) return;

        grounded = IsGrounded();

        // 1) Turn con Rigidbody (niente transform.Rotate)
        if (Mathf.Abs(_turnInput) > 0.0001f)
        {
            Quaternion delta = Quaternion.Euler(0f, _turnInput * rotationSpeed * Time.fixedDeltaTime, 0f);
            _rb.MoveRotation(_rb.rotation * delta);
        }

        // 2) Allineamento terreno con MoveRotation (niente transform.rotation = ...)
        CastGroundRaysAndAlign();

        // 3) Jump (forza) in FixedUpdate
        if (grounded && _jumpPressed)
        {
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _rb.AddForce(_rb.transform.forward * jumpForce, ForceMode.Impulse);
        }
        _jumpPressed = false;

        // 4) Sliding / boost / drag (tua logica)
        if (grounded)
        {
            _rb.linearDamping = dragOnGround;

            if (Physics.Raycast(_rb.position, -Vector3.up, out RaycastHit hit, whenIsGroundLenght, whatIsGroud))
            {
                Vector3 slopeNormal = hit.normal;
                Vector3 slopeDirection = Vector3.Cross(slopeNormal, Vector3.Cross(Vector3.down, slopeNormal)).normalized;

                // forward proiettato sulla pendenza
                Vector3 forwardOnSlope = Vector3.ProjectOnPlane(_rb.transform.forward, slopeNormal).normalized;

                float alignment = Mathf.Clamp01(Vector3.Dot(forwardOnSlope, slopeDirection));
                alignment = Mathf.Max(alignment, minAlignment);

                // throttle: stick avanti = 2x forza, centro = 1x, indietro = 0x (freno)
                float forceMultiplier = Mathf.Max(0f, 1f + Mathf.Clamp(_forwardInput, -1f, 1f));
                Vector3 slideForce = forwardOnSlope * gravityForce * alignment * forwardSpeed * forceMultiplier;
                Debug.DrawRay(_rb.position, slideForce * 5f, Color.cyan);

                _rb.AddForce(slideForce);

                if (_rb.linearVelocity.sqrMagnitude > 0.1f)
                {
                    Vector3 velDir = _rb.linearVelocity.normalized;
                    float dirAlignment = Vector3.Dot(velDir, forwardOnSlope);
                    float misalignment = 1f - Mathf.Max(0f, dirAlignment);

                    // boost release
                    if (misalignment < 0.3f)
                    {
                        _rb.AddForce(slideForce * boostAccumulato * boostForce, ForceMode.Impulse);
                        boostAccumulato = 0f;
                        driftingTime = 0f;
                    }
                    // boost decay
                    else if (_rb.linearVelocity.sqrMagnitude < speedToStartBoostDecay)
                    {
                        boostAccumulato = Mathf.Max(0f, boostAccumulato - Time.fixedDeltaTime);
                    }
                    // boost charge
                    else if (driftingTime > timeToStartBoostCharge)
                    {
                        boostAccumulato = Mathf.Min(1f, boostAccumulato + misalignment * Time.fixedDeltaTime);
                    }
                    else
                    {
                        driftingTime += Time.fixedDeltaTime;
                    }

                    _rb.linearVelocity *= Mathf.Lerp(1f, 0.9f, misalignment * Time.fixedDeltaTime * sideBreak);
                }
            }
        }
        else
        {
            _rb.linearDamping = dragInAir;
            _rb.AddForce(Vector3.down * gravityForce);
        }
    }

    private void CastGroundRaysAndAlign()
    {
        Vector3 origin = _rb.position;
        Vector3 down = -_rb.transform.up;

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
                _rb.transform.right * Mathf.Cos(angle) +
                _rb.transform.forward * Mathf.Sin(angle);

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
            if (!hasSmoothedNormal)
            {
                smoothedNormal = averagedNormal;
                hasSmoothedNormal = true;
            }

            // interpolazione morbida della normale
            smoothedNormal = Vector3.Slerp(
                smoothedNormal,
                averagedNormal,
                normalAlignSpeed * Time.fixedDeltaTime
            );

            // proietta forward sul piano definito dalla NORMALE SMUSSATA
            Vector3 forwardProjected = Vector3.ProjectOnPlane(_rb.transform.forward, smoothedNormal).normalized;

            // fallback nel caso raro in cui forwardProjected diventi quasi zero
            if (forwardProjected.sqrMagnitude < 0.0001f)
                forwardProjected = Vector3.ProjectOnPlane(_rb.transform.up, smoothedNormal).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(forwardProjected, smoothedNormal);

            // IMPORTANT: niente transform.rotation -> usa MoveRotation
            Quaternion newRot = Quaternion.Slerp(
                _rb.rotation,
                targetRotation,
                alignSpeed * Time.fixedDeltaTime
            );

            _rb.MoveRotation(newRot);

            // raggio verde: usa la normale smussata
            Debug.DrawRay(origin, smoothedNormal * 100f, Color.green);
        }
    }


    private bool IsGrounded()
    {
        return Physics.Raycast(_rb.position, -_rb.transform.up, out _, whenIsGroundLenght, whatIsGroud);
    }
}
