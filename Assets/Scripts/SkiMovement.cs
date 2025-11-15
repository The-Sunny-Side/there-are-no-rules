using UnityEngine;

public class SkiMovement : MonoBehaviour
{
    [Header("Fisica")] 
    public float gravity = 9.81f;
    [Header("Controlli")] 
    public float rotationSpeed = 90f;
    public float jumpForce = 0.5f;
    private Rigidbody rb;

    public bool isGrounded;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.D))
        {
            transform.Rotate(Vector3.up * Time.deltaTime * rotationSpeed);
        } else if (Input.GetKey(KeyCode.A))
        {
            transform.Rotate(Vector3.up * Time.deltaTime * -rotationSpeed);
        }
        else if (Input.GetKey(KeyCode.W)&& isGrounded)
        {
            
            rb.AddForce(transform.up*jumpForce,ForceMode.Impulse);
            rb.AddForce(transform.forward * jumpForce, ForceMode.Impulse);

        }
    }

   
    void FixedUpdate()
    {
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, 2f))
        {
            // direzioni della pendenza
            Vector3 slopeNormal = hit.normal;
            Vector3 slopeDirection = Vector3.Cross(slopeNormal, Vector3.Cross(Vector3.down, slopeNormal)).normalized;

            // Direzione in cui il giocatore "guarda" proiettata sul terreno
            Vector3 forwardOnSlope = Vector3.ProjectOnPlane(transform.forward, slopeNormal).normalized;

            float alignment = Mathf.Clamp01(Vector3.Dot(forwardOnSlope, slopeDirection));
            float minAlignment = 0.2f; // forza minima per slidare in pianura
            alignment = Mathf.Max(alignment, minAlignment);

            // Forza di scivolamento che segue la direzione del muso
            Vector3 slideForce = forwardOnSlope * gravity * alignment;
            Debug.DrawRay(transform.position, slideForce * 5f, Color.aliceBlue); // Debug per vedere la direzione

            rb.AddForce(slideForce, ForceMode.Acceleration);

            // Riduce la velocità se di lato rispetto alla pendenza
            if (rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                Vector3 velDir = rb.linearVelocity.normalized;
                float dirAlignment = Vector3.Dot(velDir, forwardOnSlope); // 1 = avanti, 0 = di lato, -1 = indietro
                float misalignment = 1f - Mathf.Max(0f, dirAlignment);    // 0 = allineato, 1 = perpendicolare
                rb.linearVelocity *= Mathf.Lerp(1f, 0.9f, misalignment * Time.fixedDeltaTime * 10f);
            }
           
        }
        else if (isGrounded && Vector3.Dot(transform.up, Vector3.up) < 0f)
        {
            rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);        
            rb.AddTorque(transform.forward * 0.2f, ForceMode.Impulse);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            isGrounded=true;
        }
    }
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            isGrounded = false;
        }
    }
}
