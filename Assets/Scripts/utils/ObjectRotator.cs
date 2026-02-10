using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    [SerializeField] private bool horizontalRotation = true;
    [SerializeField] private float rotationSpeed = 100f;

    private Vector3 lastInputPosition;
    private bool isRotating = false;
    private Camera mainCamera;


    public void CheckCollider()
    {
        Collider collider = GetComponent<Collider>();

        // Aggiungi automaticamente un Collider se non presente
        if (collider == null)
        {
            AddAppropriateCollider();
        }
        else
        {
            Destroy(collider);
            AddAppropriateCollider();
        }
    }

    void Start()
    {
        mainCamera = Camera.main;

        CheckCollider();
    }

    private void AddAppropriateCollider()
    {
        // Calcola i bounds dell'oggetto includendo tutti i renderer figli
        Bounds bounds = CalculateBounds();

        if (bounds.size == Vector3.zero)
        {
            // Se non ci sono renderer, usa un BoxCollider di default
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            Debug.LogWarning("ObjectRotator: Nessun Renderer trovato. Aggiunto BoxCollider con dimensioni di default.");
            return;
        }

        // Verifica se c'è un MeshFilter per determinare il tipo di collider
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (meshFilter != null)
        {
            // Se c'è una mesh, usa MeshCollider
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.convex = false;
            Debug.Log("ObjectRotator: Aggiunto MeshCollider automaticamente.");
        }
        else
        {
            // Usa BoxCollider e adattalo ai bounds calcolati
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();

            // Converti i bounds da world space a local space
            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = transform.InverseTransformVector(bounds.size);

            boxCollider.center = localCenter;
            boxCollider.size = new Vector3(
                Mathf.Abs(localSize.x),
                Mathf.Abs(localSize.y),
                Mathf.Abs(localSize.z)
            );

            Debug.Log($"ObjectRotator: Aggiunto BoxCollider con dimensioni {boxCollider.size} e centro {boxCollider.center}");
        }
    }

    private Bounds CalculateBounds()
    {
        // Ottieni tutti i renderer dell'oggetto e dei suoi figli
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return new Bounds(transform.position, Vector3.zero);
        }

        // Inizializza con il primo renderer
        Bounds bounds = renderers[0].bounds;

        // Espandi per includere tutti gli altri renderer
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    void Update()
    {
        // Input Mouse (PC)
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverObject(Input.mousePosition))
            {
                lastInputPosition = Input.mousePosition;
                isRotating = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isRotating = false;
        }

        if (isRotating && Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastInputPosition;
            float rotationX = 0f;
            float rotationZ = 0f;
            float rotationY = delta.x * rotationSpeed * Time.deltaTime;

            if(!horizontalRotation)
            {
                 rotationX = delta.y * rotationSpeed * Time.deltaTime;
                 rotationZ = delta.z * rotationSpeed * Time.deltaTime;
            }
            transform.Rotate(rotationX, -rotationY, 0, Space.World);
            lastInputPosition = Input.mousePosition;
        }

        // Input Touch (Android)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (IsPointerOverObject(touch.position))
                {
                    isRotating = true;
                }
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isRotating = false;
            }

            if (isRotating && touch.phase == TouchPhase.Moved)
            {
                float rotationY = touch.deltaPosition.x * rotationSpeed * Time.deltaTime;
                float rotationX = 0f;

                if (!horizontalRotation)
                {
                    rotationX = touch.deltaPosition.y * rotationSpeed * Time.deltaTime;
                }

                transform.Rotate(rotationX, -rotationY, 0, Space.World);
            }
        }
    }

    private bool IsPointerOverObject(Vector3 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            return hit.transform == transform;
        }

        return false;
    }
}