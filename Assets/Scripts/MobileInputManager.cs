using UnityEngine;

public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager instance;

    public bool rightTapped;
    public bool leftTapped;
    public bool jumpTapped;

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
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended) {
                resetInput();
            }
            else if (touch.phase == TouchPhase.Began||touch.phase==TouchPhase.Stationary||touch.phase==TouchPhase.Moved)
            {
                float screenWidth = Screen.width;
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
    void resetInput()
    {
        rightTapped = false;
        leftTapped = false;
        jumpTapped = false;
    }
}
