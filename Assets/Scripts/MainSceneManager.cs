using UnityEngine;

public class MainSceneManager : MonoBehaviour
{
    void Start()
    {
        AudioManager.Instance.PlayBackground("background_menu");
    }
}
