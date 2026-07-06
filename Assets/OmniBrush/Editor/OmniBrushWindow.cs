using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace OmniBrush.Editor
{
    /// <summary>
    /// Scatter brush: paints palette prefabs as ScatterLayer instances onto
    /// any collider (Terrain, meshes, prefabs). Ctrl = erase, Esc = stop.
    /// </summary>
    public class OmniBrushWindow : EditorWindow
    {
        [MenuItem("Tools/OmniBrush/Brush")]
        public static void Open() => GetWindow<OmniBrushWindow>("OmniBrush");

        public static void Open(ScatterLayer target)
        {
            var window = GetWindow<OmniBrushWindow>("OmniBrush");
            window.layer = target;
        }

        [SerializeField] private ScatterLayer layer;
        [SerializeField] private bool paintMode;
        [SerializeField] private float radius = 5f;
        [SerializeField] private int instancesPerStamp = 8;
        [SerializeField] private float strokeSpacing = 0.5f; // fraction of radius
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float falloff = 0.5f;
        [SerializeField] private float slopeMin;
        [SerializeField] private float slopeMax = 45f;
        [SerializeField] private bool filterHeight;
        [SerializeField] private float heightMin;
        [SerializeField] private float heightMax = 1000f;
        [SerializeField] private LayerMask surfaceMask = ~0;

        private bool strokeActive;
        private bool hasLastStamp;
        private Vector3 lastStampPos;

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            if (layer != null) layer.MarkDirty();
            SceneView.RepaintAll();
        }

        // ------------------------------------------------------------- window UI

        private void OnGUI()
        {
            layer = (ScatterLayer)EditorGUILayout.ObjectField("Layer", layer, typeof(ScatterLayer), true);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Layer")) CreateLayer();
                using (new EditorGUI.DisabledScope(layer == null))
                {
                    if (GUILayout.Button(layer != null ? $"Select ({layer.Count} instances)" : "Select"))
                        Selection.activeObject = layer;
                }
            }

            if (layer == null)
            {
                EditorGUILayout.HelpBox("Assign or create a Scatter Layer.", MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();
            var palette = (ScatterPalette)EditorGUILayout.ObjectField("Palette", layer.palette, typeof(ScatterPalette), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(layer, "OmniBrush Palette");
                layer.palette = palette;
                layer.MarkPaletteDirty();
                EditorUtility.SetDirty(layer);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Brush", EditorStyles.boldLabel);
            radius = EditorGUILayout.Slider("Radius", radius, 0.1f, 50f);
            instancesPerStamp = EditorGUILayout.IntSlider("Instances / Stamp", instancesPerStamp, 1, 64);
            strokeSpacing = EditorGUILayout.Slider("Stroke Spacing", strokeSpacing, 0.05f, 2f);
            minDistance = EditorGUILayout.Slider("Min Distance", minDistance, 0f, 20f);
            falloff = EditorGUILayout.Slider("Edge Falloff", falloff, 0f, 1f);

            EditorGUILayout.Space();
            GUILayout.Label("Filters", EditorStyles.boldLabel);
            EditorGUILayout.MinMaxSlider(new GUIContent($"Slope {slopeMin:0}–{slopeMax:0}°"), ref slopeMin, ref slopeMax, 0f, 90f);
            filterHeight = EditorGUILayout.Toggle("Filter Height", filterHeight);
            if (filterHeight)
            {
                heightMin = EditorGUILayout.FloatField("Min Y", heightMin);
                heightMax = EditorGUILayout.FloatField("Max Y", heightMax);
            }
            int concatenated = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(surfaceMask);
            concatenated = EditorGUILayout.MaskField("Surface Layers", concatenated, InternalEditorUtility.layers);
            surfaceMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(concatenated);

            EditorGUILayout.Space();
            bool canPaint = layer.palette != null && layer.palette.entries.Count > 0;
            if (!canPaint)
                EditorGUILayout.HelpBox("Assign a palette with at least one prefab entry.", MessageType.Warning);
            using (new EditorGUI.DisabledScope(!canPaint))
            {
                bool pressed = GUILayout.Toggle(paintMode && canPaint,
                    paintMode ? "PAINTING — Esc stops, Ctrl erases" : "Start Painting",
                    "Button", GUILayout.Height(32));
                if (pressed != paintMode)
                {
                    paintMode = pressed;
                    SceneView.RepaintAll();
                }
            }
        }

        private void CreateLayer()
        {
            var go = new GameObject("Scatter Layer", typeof(ScatterLayer));
            Undo.RegisterCreatedObjectUndo(go, "Create Scatter Layer");
            layer = go.GetComponent<ScatterLayer>();
        }

        // ------------------------------------------------------------ scene GUI

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!paintMode || layer == null || layer.palette == null) return;

            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                paintMode = false;
                Repaint();
                e.Use();
                return;
            }
            if (e.alt) return; // viewport navigation

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            bool hasHit = Physics.Raycast(ray, out RaycastHit hit, 100000f, surfaceMask, QueryTriggerInteraction.Ignore);
            bool erase = e.control || e.command;

            if (hasHit)
            {
                Color color = erase ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.55f, 0f);
                Handles.color = color;
                Handles.DrawWireDisc(hit.point, hit.normal, radius);
                Handles.color = new Color(color.r, color.g, color.b, 0.4f);
                Handles.DrawWireDisc(hit.point, hit.normal, radius * Mathf.Lerp(1f, 0.5f, falloff));
                sceneView.Repaint();
            }

            switch (e.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && hasHit)
                    {
                        GUIUtility.hotControl = controlId;
                        strokeActive = true;
                        hasLastStamp = false;
                        Undo.RegisterCompleteObjectUndo(layer, "OmniBrush Stroke");
                        Stamp(hit, erase);
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && strokeActive && hasHit)
                    {
                        if (!hasLastStamp || Vector3.Distance(hit.point, lastStampPos) >= Mathf.Max(0.05f, radius * strokeSpacing))
                            Stamp(hit, erase);
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == 0 && strokeActive)
                    {
                        strokeActive = false;
                        if (GUIUtility.hotControl == controlId) GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
            }
        }

        private void Stamp(RaycastHit hit, bool erase)
        {
            hasLastStamp = true;
            lastStampPos = hit.point;

            if (erase)
            {
                if (layer.RemoveInRadius(hit.point, radius) > 0)
                    EditorUtility.SetDirty(layer);
                return;
            }

            ScatterPalette palette = layer.palette;
            Vector3 n = hit.normal;
            Vector3 tangent = Vector3.Cross(n, Vector3.up);
            if (tangent.sqrMagnitude < 1e-6f) tangent = Vector3.right;
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(n, tangent);

            bool changed = false;
            for (int i = 0; i < instancesPerStamp; i++)
            {
                Vector2 disc = Random.insideUnitCircle;
                if (falloff > 0f && Random.value < falloff * disc.magnitude) continue; // edge falloff
                Vector3 candidate = hit.point + (tangent * disc.x + bitangent * disc.y) * radius;

                // Re-project each candidate onto the surface.
                Vector3 origin = candidate + n * radius;
                if (!Physics.Raycast(origin, -n, out RaycastHit surface, radius * 2f, surfaceMask, QueryTriggerInteraction.Ignore))
                    continue;

                float slope = Vector3.Angle(surface.normal, Vector3.up);
                if (slope < slopeMin || slope > slopeMax) continue;
                if (filterHeight && (surface.point.y < heightMin || surface.point.y > heightMax)) continue;
                if (minDistance > 0f && layer.HasInstanceWithin(surface.point, minDistance)) continue;

                int entryIndex = palette.PickWeightedIndex(Random.value);
                if (entryIndex < 0) continue;
                ScatterPalette.Entry entry = palette.entries[entryIndex];

                Quaternion align = Quaternion.Slerp(Quaternion.identity,
                    Quaternion.FromToRotation(Vector3.up, surface.normal), entry.alignToNormal);
                float yaw = entry.randomYaw ? Random.Range(0f, 360f) : 0f;
                Quaternion rotation = align * Quaternion.Euler(0f, yaw, 0f);
                float scale = Random.Range(entry.uniformScale.x, entry.uniformScale.y);
                Vector3 position = surface.point + rotation * Vector3.up * entry.verticalOffset;

                layer.AddInstance(new ScatterInstance
                {
                    entryIndex = entryIndex,
                    position = position,
                    rotation = rotation,
                    scale = new Vector3(scale, scale, scale),
                });
                changed = true;
            }
            if (changed) EditorUtility.SetDirty(layer);
        }
    }
}
