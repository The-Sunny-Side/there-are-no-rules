using TMPro;
using UnityEngine;

public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager instance;
    public bool rightTapped;
    public bool leftTapped;
    public bool jumpTapped;
    private float lastTapTime;
    private float doubleTapThreshold = 0.3f;

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

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                // Double click handler
                if (touch.phase == TouchPhase.Began)
                {
                    float timeSinceLastTap = Time.time - lastTapTime;

                    if (timeSinceLastTap < doubleTapThreshold && lastTapTime > 0)
                    {
                        jumpTapped = true;
                        lastTapTime = 0;
                        return;
                    }
                    else
                    {
                        lastTapTime = Time.time;
                    }
                }
            }

            // Long press handler
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
        // Mouse support to test it into the editor
        else
        {
            float screenWidth = Screen.width;
            float mouseX = Input.mousePosition.x;

            // Double click handler
            if (Input.GetMouseButtonDown(0))
            {
                float timeSinceLastClick = Time.time - lastTapTime;

                if (timeSinceLastClick < doubleTapThreshold && lastTapTime > 0)
                {
                    jumpTapped = true;
                    lastTapTime = 0;
                    return;
                }
                else
                {
                    lastTapTime = Time.time;
                }
            }

            // Long press handler
            if (Input.GetMouseButton(0))
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