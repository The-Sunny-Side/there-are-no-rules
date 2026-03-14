using UnityEngine;
using UnityEngine.InputSystem;

public class MobileInputManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField, Range(0f, 1f)] private float rotateDeadzone = 0.1f;

    public static MobileInputManager instance;
    public Vector2 rotateAxis
    {
        get
        {
            Vector2 axis = ReadRotateAxisRaw();
            if (axis.sqrMagnitude < rotateDeadzone * rotateDeadzone)
                return Vector2.zero;

            return axis;
        }
    }

    public float rotateHorizontal => rotateAxis.x;
    public float rotateVertical => rotateAxis.y;
    public bool leftHeld => leftRotateAction.IsPressed();
    public bool rightHeld => rightRotateAction.IsPressed();
    public bool slideUpHeld => slideUpAction.IsPressed();
    public bool slideDownHeld => slideDownAction.IsPressed();
    public bool slideLeftHeld => slideLeftAction.IsPressed();
    public bool slideRightHeld => slideRightAction.IsPressed();
    public bool jumpTapped => jumpAction.IsPressed();

    private InputAction rotateAction;
    private InputAction leftRotateAction;
    private InputAction rightRotateAction;
    private InputAction jumpAction;
    private InputAction slideUpAction;
    private InputAction slideLeftAction;
    private InputAction slideRightAction;
    private InputAction slideDownAction;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        var map = inputActions.FindActionMap("Match", true);
        rotateAction = map.FindAction("Rotate", false);
        leftRotateAction = map.FindAction("LeftRotate", true);
        rightRotateAction = map.FindAction("RightRotate", true);
        jumpAction = map.FindAction("Jump", true);
        slideUpAction = map.FindAction("SlideUp", true);
        slideDownAction = map.FindAction("SlideDown", true);
        slideLeftAction = map.FindAction("SlideLeft", true);
        slideRightAction = map.FindAction("SlideRight", true);
    }

    void OnEnable()
    {
        rotateAction?.Enable();
        leftRotateAction.Enable();
        rightRotateAction.Enable();
        jumpAction.Enable();
        slideUpAction.Enable();
        slideDownAction.Enable();
        slideLeftAction.Enable();   
        slideRightAction.Enable();
    }

    void OnDisable()
    {
        rotateAction?.Disable();
        leftRotateAction.Disable();
        rightRotateAction.Disable();
        jumpAction.Disable();
        slideUpAction.Disable();
        slideDownAction.Disable();
        slideLeftAction.Disable();
        slideRightAction.Disable();
    }

    private Vector2 ReadRotateAxisRaw()
    {
        float axisX = 0f;
        if (rightRotateAction.IsPressed())
            axisX += 1f;

        if (leftRotateAction.IsPressed())
            axisX -= 1f;
        if (axisX != 0f) 
            return new Vector2(axisX, 0f);

        if (rotateAction != null)
            return rotateAction.ReadValue<Vector2>();

        return new Vector2(0f, 0f);
    }
}
