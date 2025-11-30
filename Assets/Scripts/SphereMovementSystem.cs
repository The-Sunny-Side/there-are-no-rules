using UnityEngine;

public class SphereMovementSystem : MonoBehaviour
{
    private MobileInputManager inputManager;
    [Header("SFERA RIGIDBODY")]
    [SerializeField] private Rigidbody rb;


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
    [SerializeField] int raysCount = 8;
    [SerializeField] float raySpread = 0.25f;
    [SerializeField] private LayerMask whatIsGroud;
    [SerializeField] private float groundRayLength = .5f;
    [SerializeField] private float whenIsGroundLenght = .5f;
    [SerializeField] private Transform groundRayPoint;

    public float boostAccumulato = 0f;
    public bool grounded;
    public float driftingTime = 0f;
    private Vector3 smoothedNormal = Vector3.up;
    private bool hasSmoothedNormal = false;

    void Start()
    {
        inputManager = MobileInputManager.instance;
        rb.transform.parent = null;
    }
    void Update()
    {
        grounded = IsGrounded();

        if (inputManager.rightHeld)
        {
            transform.Rotate(Vector3.up * Time.deltaTime * rotationSpeed);
        }
        if (inputManager.leftHeld)
        {
            transform.Rotate(Vector3.up * Time.deltaTime * -rotationSpeed);
        }

        if (grounded && inputManager.jumpTapped)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            rb.AddForce(transform.forward * jumpForce, ForceMode.Impulse);
        }

        transform.position = rb.transform.position;
    }

    void CastGroundRays()
    {

        Vector3 origin = groundRayPoint.position;
        Vector3 down = -transform.up;

        float angleStep = 360f / raysCount;

        // accumulo per la media ponderata
        Vector3 weightedNormalSum = Vector3.zero;
        float weightSum = 0f;
        bool foundHit = false;

        for (int i = 0; i < raysCount; i++)
        {
            // angolo sul piano XZ (locale)
            float angle = angleStep * i * Mathf.Deg2Rad;

            // direzione “laterale” intorno all’oggetto (sul piano orizzontale)
            Vector3 radial =
                transform.right * Mathf.Cos(angle) +
                transform.forward * Mathf.Sin(angle);

            // direzione finale del raggio: verso il basso ma leggermente inclinata
            Vector3 dir = (down + radial * raySpread).normalized;

            RaycastHit hit;
            if (Physics.Raycast(origin, dir, out hit, groundRayLength, whatIsGroud))
            {
                foundHit = true;

                // disegna il raggio che colpisce in giallo
                Debug.DrawRay(origin, dir * hit.distance, Color.yellow);

                // ---- PESO IN BASE ALLA VICINANZA ----
                // distanza normalizzata [0..1] rispetto alla lunghezza massima del raggio
                float t = hit.distance / groundRayLength;      // 0 = vicinissimo, 1 = al limite
                float weight = 1f - Mathf.Clamp01(t);          // 1 = vicino, 0 = lontano

                // facoltativo: rendilo ancora più "pesante" per i molto vicini
                // weight *= weight;

                weightedNormalSum += hit.normal * weight;
                weightSum += weight;
            }
            else
            {
                Debug.DrawRay(origin, dir * groundRayLength, Color.red);
            }
        }

        // dopo aver controllato tutti i raggi, calcoliamo la normale mediata
        if (foundHit && weightSum > 0.0001f)
        {
            // normale media ponderata istantanea
            Vector3 averagedNormal = (weightedNormalSum / weightSum).normalized;

            // inizializza la smoothedNormal al primo frame utile
            if (!hasSmoothedNormal)
            {
                smoothedNormal = averagedNormal;
                hasSmoothedNormal = true;
            }

            // interpolazione morbida della normale (il "raggio verde" si muove gradualmente)
            smoothedNormal = Vector3.Slerp(
                smoothedNormal,
                averagedNormal,
                normalAlignSpeed * Time.fixedDeltaTime
            );

            // proiettare il forward sul piano definito dalla NORMALE SMUSSATA
            Vector3 forwardProjected = Vector3.ProjectOnPlane(transform.forward, smoothedNormal).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(forwardProjected, smoothedNormal);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                alignSpeed * Time.fixedDeltaTime
            );

            // raggio verde: usa la normale smussata
            Debug.DrawRay(origin, smoothedNormal * 100f, Color.green);
        }
    }


    void FixedUpdate()
    {
        CastGroundRays();
        // Fisica della pendenza - solo se a terra
        if (grounded)
        {
            rb.linearDamping = dragOnGround;
            if (Physics.Raycast(groundRayPoint.position, -Vector3.up, out RaycastHit hit, whenIsGroundLenght, whatIsGroud))
            {
                // direzioni della pendenza
                Vector3 slopeNormal = hit.normal;
                Vector3 slopeDirection = Vector3.Cross(slopeNormal, Vector3.Cross(Vector3.down, slopeNormal)).normalized;
                // Direzione in cui il giocatore "guarda" proiettata sul terreno
                Vector3 forwardOnSlope = Vector3.ProjectOnPlane(transform.forward, slopeNormal).normalized;
                float alignment = Mathf.Clamp01(Vector3.Dot(forwardOnSlope, slopeDirection));
                alignment = Mathf.Max(alignment, minAlignment);
                // Forza di scivolamento che segue la direzione del muso
                Vector3 slideForce = forwardOnSlope * gravityForce * alignment * forwardSpeed;

                Debug.DrawRay(transform.position, slideForce * 5f, Color.aliceBlue); // Debug per vedere la direzione
                rb.AddForce(slideForce);
                // Riduce la velocità se di lato rispetto alla pendenza
                if (rb.linearVelocity.sqrMagnitude > 0.1f)
                {
                    Vector3 velDir = rb.linearVelocity.normalized;
                    float dirAlignment = Vector3.Dot(velDir, forwardOnSlope); // 1 = avanti, 0 = di lato, -1 = indietro
                    float misalignment = 1f - Mathf.Max(0f, dirAlignment);    // 0 = allineato, 1 = perpendicolare
                    //boost release
                    if (misalignment < 0.3)
                    {
                        rb.AddForce(slideForce * boostAccumulato * boostForce, ForceMode.Impulse);


                        boostAccumulato = 0f;
                        driftingTime = 0f;

                    }
                    //boost decay
                    else if (rb.linearVelocity.sqrMagnitude < speedToStartBoostDecay)
                    {
                        boostAccumulato = Mathf.Max(0, boostAccumulato - Time.fixedDeltaTime);
                    }
                    //boost charge
                    else if (driftingTime > timeToStartBoostCharge)
                    {

                        boostAccumulato = Mathf.Min(1, boostAccumulato + misalignment * Time.fixedDeltaTime);
                        //carica boost
                    }
                    else
                    {
                        driftingTime += Time.fixedDeltaTime;
                    }


                    rb.linearVelocity *= Mathf.Lerp(1f, 0.9f, misalignment * Time.fixedDeltaTime * sideBreak);

                }

            }

        }


        if (!grounded)
        {
            rb.linearDamping = dragInAir;
            rb.AddForce(Vector3.down * gravityForce);

        }

    }

    private bool IsGrounded()
    {
        return Physics.Raycast(groundRayPoint.position, -Vector3.up, out RaycastHit hit, whenIsGroundLenght, whatIsGroud);
    }
}