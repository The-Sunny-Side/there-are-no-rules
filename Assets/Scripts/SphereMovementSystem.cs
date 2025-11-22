using UnityEngine;

public class SphereMovementSystem : MonoBehaviour
{
    [SerializeField] private float gravityForce = 100f;
    public float rotationSpeed = 90f;
    [SerializeField] private float speed;
    
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float dragOnGround=3f;

    private float speedInput = 0;
    public bool grounded;

    public LayerMask whatIsGroud;
    public float groundRayLength = .5f;
    public Transform groundRayPoint;

    void Start()
    {
        //rb = GetComponent<Rigidbody>();
        rb.transform.parent=null;
    }

    void Update()
    {

        if (Input.GetKey(KeyCode.W))
        {
            speedInput=speed;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            speedInput=-speed;
        }
        else
        {
            speedInput=0;
        }


        if (Input.GetKey(KeyCode.D))
        {
            transform.Rotate(Vector3.up * Time.deltaTime * rotationSpeed);
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.Rotate(Vector3.up * Time.deltaTime * -rotationSpeed);
        }

        transform.position =
            new Vector3(rb.transform.position.x, rb.transform.position.y - 1.6f, rb.transform.position.z);


    }

    void FixedUpdate()
    {
        grounded = false;
        RaycastHit hit;
        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLength, whatIsGroud))
        {
            grounded = true;
            transform.rotation=Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;

        }

        if (grounded)
        {
            rb.linearDamping=dragOnGround;
            rb.AddForce(transform.forward * speedInput);
        }
        else
        {
            rb.linearDamping=0.1f;
            rb.AddForce(-transform.up * gravityForce);
        }
    }

   
}