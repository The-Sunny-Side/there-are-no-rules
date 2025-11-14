using UnityEngine;

public class MainSceneManager : MonoBehaviour
{
    void Start()
    {
        AudioManager.Instance.PlayBackground("background_menu", 0.5f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            AudioManager.Instance.PlayOneShot("notification_ok", 1.0f);
        }
    }
}
