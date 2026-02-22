using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class SwipeDetector : MonoBehaviour
{
    public float minSwipeDistance = 50f;
    [SerializeField] private VehicleWeaponSystem weaponSystem;
    private Vector2 startPos;
    private bool swiping;

    public bool swipedLeft = false;
    public bool swipedRight = false;

    public bool swipedUp = false;

    public bool swipedDown = false;

    void Start()
    {
        if (weaponSystem == null)
        {
            weaponSystem = GetComponent<VehicleWeaponSystem>();
        }
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                startPos = touch.screenPosition;
                swiping = true;
            }
            else if (swiping && touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
            {
                swiping = false;
                Vector2 delta = touch.screenPosition - startPos;
                if (delta.magnitude < minSwipeDistance) return;

                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    if (delta.x > 0) OnSwipeRight();
                    else OnSwipeLeft();
                }
                else
                {
                    if (delta.y > 0) OnSwipeUp();
                    else OnSwipeDown();
                }
            }
        }
    }

    void OnSwipeLeft()
    {
        swipedLeft = true;
    }
    void OnSwipeRight()
    {
        swipedRight = true;
    }
    void OnSwipeUp()
    {
        swipedUp = true;
    }
    void OnSwipeDown()
    {
        swipedDown = true;
    }
}