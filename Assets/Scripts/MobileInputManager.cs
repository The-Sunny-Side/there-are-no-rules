using TMPro;
using UnityEngine;

public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager instance;
    public bool rightTapped;
    public bool leftTapped;
    public bool jumpTapped;
    private float lastTapTime;
    private float doubleTapThreshold = 0.2f;
    private bool shouldIgnoreMovement = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Update()
    {
        ReadInput();
    }

    void ResetInput()
    {
        rightTapped = false;
        leftTapped = false;
        jumpTapped = false;
    }

    void ReadInput()
    {
        ResetInput();

        if (Input.touchCount > 0)
        {
            float screenWidth = Screen.width;
            bool isDoubleTap = false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                // Double tap handler
                if (touch.phase == TouchPhase.Began)
                {
                    float timeSinceLastTap = Time.time - lastTapTime;

                    if (timeSinceLastTap < doubleTapThreshold && lastTapTime > 0)
                    {
                        // Double tap detected
                        jumpTapped = true;
                        isDoubleTap = true;
                        shouldIgnoreMovement = false;
                        lastTapTime = 0;
                        return;
                    }
                    else
                    {
                        // First tap - waiting for the second
                        lastTapTime = Time.time;
                        shouldIgnoreMovement = true;
                    }
                }
            }

            // Reset the flag if threshold time is passed (no double tap)
            if (shouldIgnoreMovement && (Time.time - lastTapTime) > doubleTapThreshold)
            {
                shouldIgnoreMovement = false;
            }

            // Long press handler - only if we are not waiting for a second tap
            if (!shouldIgnoreMovement && !isDoubleTap)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    float touchX = touch.position.x;

                    if (touchX < screenWidth / 2)
                    {
                        leftTapped = true;
                    }
                    else
                    {
                        rightTapped = true;
                    }
                }
            }
        }
        else
        {
            // Reset when there are no touches
            shouldIgnoreMovement = false;
        }

        // Mouse support to test it into the editor
        if (Input.touchCount == 0)
        {
            float screenWidth = Screen.width;
            float mouseX = Input.mousePosition.x;
            bool isDoubleTap = false;

            // Double click handler
            if (Input.GetMouseButtonDown(0))
            {
                float timeSinceLastClick = Time.time - lastTapTime;

                if (timeSinceLastClick < doubleTapThreshold && lastTapTime > 0)
                {
                    jumpTapped = true;
                    isDoubleTap = true;
                    shouldIgnoreMovement = false;
                    lastTapTime = 0;
                    return;
                }
                else
                {
                    lastTapTime = Time.time;
                    shouldIgnoreMovement = true;
                }
            }

            // Reset the flag if threshold time is passed
            if (shouldIgnoreMovement && (Time.time - lastTapTime) > doubleTapThreshold)
            {
                shouldIgnoreMovement = false;
            }

            // Long press handler
            if (Input.GetMouseButton(0) && !shouldIgnoreMovement && !isDoubleTap)
            {
                if (mouseX < screenWidth / 2)
                {
                    leftTapped = true;
                }
                else
                {
                    rightTapped = true;
                }
            }
        }
    }
}