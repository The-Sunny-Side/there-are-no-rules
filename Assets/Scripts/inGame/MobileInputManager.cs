using UnityEngine;
using UnityEngine.InputSystem;

public class MobileInputManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    public static MobileInputManager instance;
    public bool leftHeld => leftRotateAction.IsPressed();
    public bool rightHeld => rightRotateAction.IsPressed();
    public bool slideUpHeld => slideUpAction.IsPressed();
    public bool slideDownHeld => slideDownAction.IsPressed();
    public bool slideLeftHeld => slideLeftAction.IsPressed();
    public bool slideRightHeld => slideRightAction.IsPressed();
    public bool jumpTapped => jumpAction.IsPressed();

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
        leftRotateAction.Disable();
        rightRotateAction.Disable();
        jumpAction.Disable();
        slideUpAction.Disable();
        slideDownAction.Disable();
        slideLeftAction.Disable();
        slideRightAction.Disable();
    }
}