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
                "Move points with the scene handles. SHIFT+CLICK in the scene appends a point where you click. Apply road/river ops from Tools > OmniBrush > Brush, Spline tab.",
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

            Event e = Event.current;
            if (e.shift)
            {
                // Shift held: click appends a point at the surface under the cursor
                int controlId = GUIUtility.GetControlID(FocusType.Passive);
                HandleUtility.AddDefaultControl(controlId);
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                bool valid = Physics.Raycast(ray, out RaycastHit hit, 100000f);
                Vector3 point = valid ? hit.point : Vector3.zero;
                if (!valid)
                {
                    var plane = new Plane(Vector3.up, spline.GetWorldPoint(spline.PointCount - 1));
                    if (plane.Raycast(ray, out float distance))
                    {
                        point = ray.GetPoint(distance);
                        valid = true;
                    }
                }
                if (valid)
                {
                    Handles.color = Color.green;
                    Handles.DrawWireDisc(point, Vector3.up, 0.5f);
                    Handles.DrawDottedLine(spline.GetWorldPoint(spline.PointCount - 1), point, 4f);
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        Undo.RecordObject(spline, "Add Spline Point");
                        spline.localPoints.Add(spline.transform.InverseTransformPoint(point));
                        EditorUtility.SetDirty(spline);
                        e.Use();
                    }
                    HandleUtility.Repaint();
                }
                return; // no position handles while adding
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
