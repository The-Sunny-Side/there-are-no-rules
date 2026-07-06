using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace OmniBrush.Editor
{
    /// <summary>
    /// OmniBrush main window. Scatter mode paints palette prefabs as
    /// ScatterLayer instances onto any collider; Sculpt mode deforms terrain
    /// (raise/lower/smooth/flatten). Ctrl = erase / invert, Esc = stop.
    /// </summary>
    public class OmniBrushWindow : EditorWindow
    {
        private enum Mode { Scatter, Sculpt }

        [MenuItem("Tools/OmniBrush/Brush")]
        public static void Open() => GetWindow<OmniBrushWindow>("OmniBrush");

        public static void Open(ScatterLayer target)
        {
            var window = GetWindow<OmniBrushWindow>("OmniBrush");
            window.layer = target;
            window.mode = Mode.Scatter;
        }

        [SerializeField] private Mode mode;
        [SerializeField] private bool paintMode;
        [SerializeField] private float radius = 5f;
        [SerializeField] private LayerMask surfaceMask = ~0;

        // scatter
        [SerializeField] private ScatterLayer layer;
        [SerializeField] private int instancesPerStamp = 8;
        [SerializeField] private float strokeSpacing = 0.5f; // fraction of radius
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float falloff = 0.5f;
        [SerializeField] private float slopeMin;
        [SerializeField] private float slopeMax = 45f;
        [SerializeField] private bool filterHeight;
        [SerializeField] private float heightMin;
        [SerializeField] private float heightMax = 1000f;

        // sculpt
        [SerializeField] private SculptOp sculptOp = SculptOp.Raise;
        [SerializeField] private float sculptStrength = 0.5f;
        [SerializeField] private float sculptHardness = 0.5f;

        private bool strokeActive;
        private bool hasLastStamp;
        private Vector3 lastStampPos;
        private bool sculptStrokeStarted;
        private float flattenTargetY;

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedo;
            if (sculptStrokeStarted) SculptUndo.EndStroke();
        }

        private void OnUndoRedo()
        {
            if (layer != null) layer.MarkDirty();
            SceneView.RepaintAll();
        }

        // ------------------------------------------------------------- window UI

        private void OnGUI()
        {
            var newMode = (Mode)GUILayout.Toolbar((int)mode, new[] { "Scatter", "Sculpt" }, GUILayout.Height(24));
            if (newMode != mode)
            {
                mode = newMode;
                SceneView.RepaintAll();
            }
            EditorGUILayout.Space();

            radius = EditorGUILayout.Slider("Radius", radius, 0.1f, 50f);
            int concatenated = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(surfaceMask);
            concatenated = EditorGUILayout.MaskField("Surface Layers", concatenated, InternalEditorUtility.layers);
            surfaceMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(concatenated);
            EditorGUILayout.Space();

            bool canPaint = mode == Mode.Sculpt ? DrawSculptGUI() : DrawScatterGUI();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!canPaint))
            {
                string activeLabel = mode == Mode.Sculpt
                    ? "PAINTING — Esc stops, Ctrl inverts"
                    : "PAINTING — Esc stops, Ctrl erases";
                bool pressed = GUILayout.Toggle(paintMode && canPaint,
                    paintMode ? activeLabel : "Start Painting", "Button", GUILayout.Height(32));
                if (pressed != paintMode)
                {
                    paintMode = pressed;
                    SceneView.RepaintAll();
                }
            }
        }

        private bool DrawScatterGUI()
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
                return false;
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

            bool canPaint = layer.palette != null && layer.palette.entries.Count > 0;
            if (!canPaint)
                EditorGUILayout.HelpBox("Assign a palette with at least one prefab entry.", MessageType.Warning);
            return canPaint;
        }

        private bool DrawSculptGUI()
        {
            sculptOp = (SculptOp)GUILayout.Toolbar((int)sculptOp, new[] { "Raise", "Lower", "Smooth", "Flatten" });
            sculptStrength = EditorGUILayout.Slider("Strength", sculptStrength, 0f, 1f);
            sculptHardness = EditorGUILayout.Slider("Hardness", sculptHardness, 0f, 1f);
            EditorGUILayout.HelpBox(
                "Terrain only (mesh sculpt comes later). Ctrl inverts Raise/Lower. Flatten targets the height first clicked.",
                MessageType.None);
            return true;
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
            if (!paintMode) return;
            if (mode == Mode.Scatter && (layer == null || layer.palette == null)) return;

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
            bool modifier = e.control || e.command;

            if (hasHit)
            {
                bool destructive = mode == Mode.Scatter && modifier;
                Color color = destructive ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.55f, 0f);
                Handles.color = color;
                Handles.DrawWireDisc(hit.point, hit.normal, radius);
                Handles.color = new Color(color.r, color.g, color.b, 0.4f);
                float inner = mode == Mode.Scatter ? Mathf.Lerp(1f, 0.5f, falloff) : Mathf.Lerp(0.5f, 1f, sculptHardness);
                Handles.DrawWireDisc(hit.point, hit.normal, radius * inner);
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
                        if (mode == Mode.Scatter)
                        {
                            Undo.RegisterCompleteObjectUndo(layer, "OmniBrush Stroke");
                            ScatterStamp(hit, modifier);
                        }
                        else
                        {
                            sculptStrokeStarted = false;
                            flattenTargetY = hit.point.y;
                            SculptStamp(hit, modifier);
                        }
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && strokeActive && hasHit)
                    {
                        if (mode == Mode.Scatter)
                        {
                            if (!hasLastStamp || Vector3.Distance(hit.point, lastStampPos) >= Mathf.Max(0.05f, radius * strokeSpacing))
                                ScatterStamp(hit, modifier);
                        }
                        else
                        {
                            SculptStamp(hit, modifier);
                        }
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == 0 && strokeActive)
                    {
                        strokeActive = false;
                        if (sculptStrokeStarted)
                        {
                            SculptUndo.EndStroke();
                            sculptStrokeStarted = false;
                        }
                        if (GUIUtility.hotControl == controlId) GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
            }
        }

        // --------------------------------------------------------------- sculpt

        private void SculptStamp(RaycastHit hit, bool invert)
        {
            IPaintableSurface surface = TerrainPaintableSurface.TryFrom(hit.collider);
            if (surface == null) return; // mesh sculpt arrives in S4

            if (!sculptStrokeStarted)
            {
                SculptUndo.BeginStroke();
                sculptStrokeStarted = true;
            }

            SculptOp op = sculptOp;
            if (invert && op == SculptOp.Raise) op = SculptOp.Lower;
            else if (invert && op == SculptOp.Lower) op = SculptOp.Raise;

            surface.ApplyStamp(new SculptStampArgs
            {
                op = op,
                center = hit.point,
                radius = radius,
                strength = sculptStrength,
                hardness = sculptHardness,
                flattenHeight = flattenTargetY,
            });
        }

        // -------------------------------------------------------------- scatter

        private void ScatterStamp(RaycastHit hit, bool erase)
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
