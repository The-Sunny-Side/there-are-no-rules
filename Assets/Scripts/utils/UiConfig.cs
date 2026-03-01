using UnityEngine;

[CreateAssetMenu(menuName = "Config/UI Config")]
public class UiConfig : ScriptableObject
{
    public Color baseColor = new Color(1f, 0.843f, 0.208f, 1f);
    public Color selectedColor = new Color(0.318f, 0.706f, 0.984f, 1f);
    public Color panelColor = new Color(0.318f, 0.706f, 0.984f, 1f);
}
