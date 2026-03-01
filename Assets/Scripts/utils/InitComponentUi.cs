using UnityEngine;
using UnityEngine.UI;

public enum UiComponentType
{
    Button,
    Panel
}

public class InitComponentUi : MonoBehaviour
{

    [SerializeField] private UiComponentType componentType = UiComponentType.Button;
    [SerializeField] private bool isSelected = false;

    private void Awake()
    {
        Image componentImage = GetComponent<Image>();

        if (componentImage != null)
        {
            switch (componentType)
            {
                case UiComponentType.Button:
                    componentImage.color = isSelected? Config.UiConfig.selectedColor: Config.UiConfig.baseColor;
                    break;
                case UiComponentType.Panel:
                    componentImage.color = Config.UiConfig.panelColor;
                    break;
            }
        }
    }
    
}
