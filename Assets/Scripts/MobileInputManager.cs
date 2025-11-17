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

    void ReadInput()
    {
        // Reset input
        rightTapped = false;
        leftTapped = false;
        jumpTapped = false;

        if (Input.touchCount > 0)
        {
            float screenWidth = Screen.width;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                // Rileva doppio tap
                if (touch.phase == TouchPhase.Began)
                {
                    float timeSinceLastTap = Time.time - lastTapTime;

                    if (timeSinceLastTap < doubleTapThreshold && lastTapTime > 0)
                    {
                        jumpTapped = true;
                        lastTapTime = 0;
                    }
                    else
                    {
                        lastTapTime = Time.time;
                    }
                }

                // Rileva tocchi continui per movimento (non solo Began)
                if (!jumpTapped)
                {
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
        // Mouse per testare nell'editor
        else
        {
            float screenWidth = Screen.width;
            float mouseX = Input.mousePosition.x;

            // Gestione click continuo
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

            // Gestione doppio click
            if (Input.GetMouseButtonDown(0))
            {
                float timeSinceLastClick = Time.time - lastTapTime;

                if (timeSinceLastClick < doubleTapThreshold && lastTapTime > 0)
                {
                    jumpTapped = true;
                    lastTapTime = 0;
                }
                else
                {
                    lastTapTime = Time.time;
                }
            }
        }
    }
}