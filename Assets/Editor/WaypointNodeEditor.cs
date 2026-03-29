using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaypointNode))]
public class WaypointNodeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("nodeType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bankAngle"));

        EditorGUILayout.Space();
        SerializedProperty overrideProp = serializedObject.FindProperty("overrideTension");
        EditorGUILayout.PropertyField(overrideProp, new GUIContent("Override Tension"));

        if (overrideProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("splineTension"), new GUIContent("Spline Tension"));
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
