using UnityEngine;

public class PcInputManager : MonoBehaviour
{
    public static PcInputManager instance;

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
        resetInput();
        ReadInput();
    }
    void ReadInput()
    {
        rightTapped = Input.GetKey(KeyCode.D);
        leftTapped = Input.GetKey(KeyCode.A);
        jumpTapped = Input.GetKey(KeyCode.W);
    }
    void resetInput()
    {
        rightTapped = false;
        leftTapped = false;
        jumpTapped = false;
    }
}
