using UnityEngine;

public class AnchorObject : MonoBehaviour
{
    [AnchorNameDropdown]
    public string anchorName;

    private void OnValidate()
    {
        if (!string.IsNullOrEmpty(anchorName))
        {
            gameObject.name = anchorName;
        }
    }
}

public class AnchorNameDropdownAttribute : PropertyAttribute { }