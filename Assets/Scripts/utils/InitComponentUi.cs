using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UiComponentType
{
    Button,
    Panel,
    TextTitle
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
            if (componentText != null && Config.UiConfig)
            {
                componentText.color = Config.UiConfig.baseColor;
            }
        }

        else
        {
            Image componentImage = GetComponent<Image>();

            if (componentImage != null && Config.UiConfig)
            {
                switch (componentType)
                {
                    case UiComponentType.Button:
                        componentImage.color = isSelected ? Config.UiConfig.selectedColor : Config.UiConfig.baseColor;
                        break;
                    case UiComponentType.Panel:
                        componentImage.color = Config.UiConfig.panelColor;
                        break;
                }
            }
        }
    }
    
}
