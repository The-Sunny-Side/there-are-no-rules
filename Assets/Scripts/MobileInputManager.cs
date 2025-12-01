using UnityEngine;
using UnityEngine.InputSystem;

public class MobileInputManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    public static MobileInputManager instance;
    public bool leftHeld => leftRotateAction.IsPressed();
    public bool rightHeld => rightRotateAction.IsPressed();

    public bool jumpTapped => jumpAction.WasPressedThisFrame();

    private InputAction leftRotateAction;
    private InputAction rightRotateAction;
    private InputAction jumpAction;

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