using UnityEngine;
using UnityEngine.InputSystem;

public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager instance;

    [SerializeField] private InputActionAsset inputActions;

    private InputAction leftRotateAction;
    private InputAction rightRotateAction;
    private InputAction jumpAction;

    public bool leftHeld => leftRotateAction.IsPressed();
    public bool rightHeld => rightRotateAction.IsPressed();

    public bool jumpTapped => jumpAction.WasPressedThisFrame();

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
    }

    void OnEnable()
    {
        leftRotateAction.Enable();
        rightRotateAction.Enable();
        jumpAction.Enable();
    }

    void OnDisable()
    {
        leftRotateAction.Disable();
        rightRotateAction.Disable();
        jumpAction.Disable();
    }
}