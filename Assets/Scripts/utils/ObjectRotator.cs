using UnityEngine;

[RequireComponent(typeof(Transform))]
public class ObjectRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private bool horizontalRotation = true;
    [SerializeField] private float rotationSpeed = 150f;

    private Camera mainCamera;
    private bool isRotating;
    private Vector2 lastPointerPosition;

    #region Unity Lifecycle

    void Awake()
    {
        mainCamera = Camera.main;
        SetupCollider();
    }

    void Update()
    {
        HandlePointerInput();
    }

    #endregion

    #region Collider Setup

    public void SetupCollider()
    {
        Collider existing = GetComponent<Collider>();
        if (existing != null)
            Destroy(existing);

        AddBoxColliderFromRenderers();
    }

    private void AddBoxColliderFromRenderers()
    {
        Bounds bounds = CalculateLocalBounds();

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = bounds.size;
    }

private Bounds CalculateLocalBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return new Bounds(Vector3.zero, Vector3.one);

        Bounds bounds = new Bounds();
        bool initialized = false;

        foreach (Renderer r in renderers)
        {
            Bounds wb = r.bounds;
            Vector3 c = wb.center;
            Vector3 e = wb.extents;

            // Gli 8 angoli in world space
            Vector3[] worldCorners =
            {
            c + new Vector3( e.x,  e.y,  e.z),
            c + new Vector3( e.x,  e.y, -e.z),
            c + new Vector3( e.x, -e.y,  e.z),
            c + new Vector3( e.x, -e.y, -e.z),
            c + new Vector3(-e.x,  e.y,  e.z),
            c + new Vector3(-e.x,  e.y, -e.z),
            c + new Vector3(-e.x, -e.y,  e.z),
            c + new Vector3(-e.x, -e.y, -e.z),
        };

            foreach (Vector3 wc in worldCorners)
            {
                // Converti il corner (già in world space) in local space
                Vector3 lc = transform.InverseTransformPoint(wc);

                if (!initialized)
                {
                    bounds = new Bounds(lc, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(lc);
                }
            }
        }

        return bounds;
    }

    #endregion

    #region Input Handling

    private void HandlePointerInput()
    {
        if (TryGetPointerDown(out Vector2 position))
        {
            if (IsPointerOverObject(position))
            {
                isRotating = true;
                lastPointerPosition = position;
            }
        }

        if (TryGetPointerUp())
        {
            isRotating = false;
        }

        if (isRotating && TryGetPointer(out Vector2 currentPosition))
        {
            Vector2 delta = currentPosition - lastPointerPosition;
            RotateObject(delta);
            lastPointerPosition = currentPosition;
        }
    }

    private void RotateObject(Vector2 delta)
    {
        float rotX = 0f;
        float rotY = delta.x * rotationSpeed * Time.deltaTime;

        if (!horizontalRotation)
            rotX = delta.y * rotationSpeed * Time.deltaTime;

        transform.Rotate(rotX, -rotY, 0f, Space.World);
    }

    #endregion

    #region Pointer Abstraction (Mouse + Touch)

    private bool TryGetPointerDown(out Vector2 position)
    {
        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        position = default;
        return false;
    }

    private bool TryGetPointerUp()
    {
        if (Input.GetMouseButtonUp(0))
            return true;

        if (Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
        }

        return false;
    }

    private bool TryGetPointer(out Vector2 position)
    {
        if (Input.GetMouseButton(0))
        {
            position = Input.mousePosition;
            return true;
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        position = default;
        return false;
    }

    #endregion

    #region Raycast

    private bool IsPointerOverObject(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.transform == transform;

        return false;
    }

    #endregion
}
