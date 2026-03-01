using UnityEngine;

public class Config : MonoBehaviour
{
    public static UiConfig UiConfig { get; private set; }

    [SerializeField] private UiConfig uiConfig;

    private void Awake()
    {
        if (UiConfig == null)
        {
            UiConfig = uiConfig;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
