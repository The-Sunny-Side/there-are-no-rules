using UnityEditor;
using UnityEngine;

namespace OmniBrush.Editor
{
    [CustomEditor(typeof(OmniSpline))]
    public class OmniSplineEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var spline = (OmniSpline)target;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Point"))
                {
                    Undo.RecordObject(spline, "Add Spline Point");
                    int n = spline.localPoints.Count;
                    Vector3 direction = n >= 2
                        ? (spline.localPoints[n - 1] - spline.localPoints[n - 2]).normalized
                        : Vector3.forward;
                    spline.localPoints.Add(spline.localPoints[n - 1] + direction * 8f);
                    EditorUtility.SetDirty(spline);
                }
                using (new EditorGUI.DisabledScope(spline.localPoints.Count <= 2))
                {
                    if (GUILayout.Button("Remove Last"))
                    {
                        Undo.RecordObject(spline, "Remove Spline Point");
                        spline.localPoints.RemoveAt(spline.localPoints.Count - 1);
                        EditorUtility.SetDirty(spline);
                    }
                }
            }
            EditorGUILayout.HelpBox(
                "Move points with the scene handles. Apply road/river ops from Tools > OmniBrush > Brush, Spline tab.",
                MessageType.None);
        }

        private void OnSceneGUI()
        {
            var spline = (OmniSpline)target;
            if (spline.PointCount >= 2)
            {
                var samples = spline.SampleByDistance(1f);
                Handles.color = new Color(1f, 0.55f, 0f);
                Handles.DrawAAPolyLine(3f, samples.ToArray());
            }
            for (int i = 0; i < spline.PointCount; i++)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 world = Handles.PositionHandle(spline.GetWorldPoint(i), Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(spline, "Move Spline Point");
                    spline.SetWorldPoint(i, world);
                    EditorUtility.SetDirty(spline);
                }
            }
        }
    }
}
