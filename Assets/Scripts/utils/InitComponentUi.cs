using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UiComponentType
{
    Primary,
    Secondary,
    Stick,
    Button,
    Panel,
    TextTitle,
    Yellow,
    Blue,
}

public class InitComponentUi : MonoBehaviour
{

    [SerializeField] private UiComponentType componentType = UiComponentType.Button;
    [SerializeField] private bool isSelected = false;

    private void Awake()
    {
        if (componentType == UiComponentType.TextTitle)
        {
            TextMeshProUGUI componentText = GetComponent<TextMeshProUGUI>();
            if (componentText != null && GameConfig.UiConfig)
            {
                componentText.color = GameConfig.UiConfig.baseColor;
            }
        }

        else
        {
            Image componentImage = GetComponent<Image>();

            if (componentImage != null && GameConfig.UiConfig)
            {
                switch (componentType)
                {
                    case UiComponentType.Button:
                        componentImage.color = isSelected ? GameConfig.UiConfig.selectedColor : GameConfig.UiConfig.baseColor;
                        break;
                    case UiComponentType.Panel:
                        componentImage.color = GameConfig.UiConfig.panelColor;
                        break;
                    case UiComponentType.Primary:
                        componentImage.color = GameConfig.UiConfig.primaryColor;
                        break;
                    case UiComponentType.Secondary:
                        componentImage.color = GameConfig.UiConfig.secondaryColor;
                        break;
                    case UiComponentType.Stick:
                        componentImage.color = GameConfig.UiConfig.stickColor;
                        break;
                }
            }
        }
    }
    
}
