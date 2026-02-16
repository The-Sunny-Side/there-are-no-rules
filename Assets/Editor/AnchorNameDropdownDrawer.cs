using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomPropertyDrawer(typeof(AnchorNameDropdownAttribute))]
public class AnchorNameDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Ottieni le label leggibili e i valori reali
        var displayNames = AnchorNames.DisplayNames;
        string[] labels = displayNames.Keys.ToArray();
        string[] values = displayNames.Values.ToArray();

        // Trova l'indice corrente basato sul valore dell'anchor
        int currentIndex = System.Array.IndexOf(values, property.stringValue);
        if (currentIndex == -1) currentIndex = 0;

        // Mostra il dropdown con le label leggibili
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, labels);

        // Assegna il valore reale dell'anchor (non la label)
        property.stringValue = values[newIndex];
    }
}