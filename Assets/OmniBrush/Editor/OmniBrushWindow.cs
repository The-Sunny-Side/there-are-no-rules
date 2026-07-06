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
        private enum Mode { Scatter, Sculpt, Texture, Grass }

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

        [SerializeField] private GameObject quickAddCandidate;

        // texture paint
        [SerializeField] private TerrainLayer terrainLayer;

        // grass / detail paint
        [SerializeField] private Texture2D grassTexture;
        [SerializeField] private GameObject grassPrefab;
        [SerializeField] private int grassDensity = 8;

        // stamp
        [SerializeField] private Texture2D stampTexture;
        [SerializeField] private float stampHeight = 20f;
        [SerializeField] private bool stampAdditive;
        [SerializeField] private float stampRotation;
        [SerializeField] private bool stampRandomRotation = true;
        private float currentStampRotation;

        private bool strokeActive;
        private bool hasLastStamp;
        private Vector3 lastStampPos;
        private string lastStampWarning;
        private bool sculptStrokeStarted;
        private Vector3 flattenPoint;
        private Vector3 flattenNormal;
        private readonly System.Collections.Generic.HashSet<MeshFilter> touchedMeshes = new System.Collections.Generic.HashSet<MeshFilter>();

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
            var newMode = (Mode)GUILayout.Toolbar((int)mode, new[] { "Scatter", "Sculpt", "Texture", "Grass" }, GUILayout.Height(24));
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

            bool canPaint = mode == Mode.Sculpt ? DrawSculptGUI()
                : mode == Mode.Texture ? DrawTextureGUI()
                : mode == Mode.Grass ? DrawGrassGUI()
                : DrawScatterGUI();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!canPaint))
            {
                string activeLabel = mode == Mode.Sculpt ? "PAINTING — Esc stops, Ctrl inverts"
                    : mode == Mode.Texture ? "PAINTING — Esc stops"
                    : "PAINTING — Esc stops, Ctrl erases"; // scatter & grass
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
            if (layer == null) layer = FindFirstObjectByType<ScatterLayer>(); // recover from Missing/scene change

            layer = (ScatterLayer)EditorGUILayout.ObjectField("Scatter Layer (scene)", layer, typeof(ScatterLayer), true);
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
                EditorGUILayout.HelpBox(
                    "Painted instances live in a Scatter Layer: one scene object holding pure data (no GameObject per instance, GPU-instanced rendering). Create one to start.",
                    MessageType.Info);
                return false;
            }

            EditorGUI.BeginChangeCheck();
            var palette = (ScatterPalette)EditorGUILayout.ObjectField("Palette (asset)", layer.palette, typeof(ScatterPalette), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(layer, "OmniBrush Palette");
                layer.palette = palette;
                layer.MarkPaletteDirty();
                EditorUtility.SetDirty(layer);
            }
            if (layer.palette == null)
            {
                if (GUILayout.Button("Create Palette Asset"))
                {
                    string path = EditorUtility.SaveFilePanelInProject("Create Scatter Palette", "ScatterPalette", "asset", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var newPalette = CreateInstance<ScatterPalette>();
                        AssetDatabase.CreateAsset(newPalette, path);
                        AssetDatabase.SaveAssets();
                        Undo.RecordObject(layer, "OmniBrush Palette");
                        layer.palette = newPalette;
                        EditorUtility.SetDirty(layer);
                    }
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    quickAddCandidate = (GameObject)EditorGUILayout.ObjectField("Quick Add Prefab", quickAddCandidate, typeof(GameObject), true);
                    using (new EditorGUI.DisabledScope(quickAddCandidate == null))
                    {
                        if (GUILayout.Button("Add", GUILayout.Width(44)))
                            QuickAddToPalette();
                    }
                }
                if (GUILayout.Button("Edit Palette Asset (weights, scale, alignment)"))
                {
                    Selection.activeObject = layer.palette;
                    EditorGUIUtility.PingObject(layer.palette);
                }
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
            {
                EditorGUILayout.HelpBox(
                    "The palette is the asset listing what to paint (prefabs with weights, scale ranges, alignment). Add at least one prefab — use Quick Add above or edit the palette asset.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Palette Entries", layer.palette.entries.Count.ToString());

                int zeroWeight = 0, nullPrefabs = 0;
                float totalWeight = 0f;
                foreach (ScatterPalette.Entry entry in layer.palette.entries)
                {
                    if (entry.prefab == null) { nullPrefabs++; continue; }
                    totalWeight += Mathf.Max(0f, entry.weight);
                    if (entry.weight <= 0f) zeroWeight++;
                }
                if (totalWeight <= 0f)
                {
                    EditorGUILayout.HelpBox("All entries have weight 0 — nothing can be painted. Weight is the relative pick probability; set it > 0 in the palette.", MessageType.Error);
                    canPaint = false;
                }
                else if (zeroWeight > 0)
                {
                    EditorGUILayout.HelpBox($"{zeroWeight} entr{(zeroWeight == 1 ? "y has" : "ies have")} weight 0 and will never be painted.", MessageType.Warning);
                }
                if (nullPrefabs > 0)
                    EditorGUILayout.HelpBox($"{nullPrefabs} entr{(nullPrefabs == 1 ? "y is" : "ies are")} missing a prefab.", MessageType.Warning);

                if (!string.IsNullOrEmpty(lastStampWarning))
                    EditorGUILayout.HelpBox(lastStampWarning, MessageType.Warning);

                int missing = ScatterMaterialUtility.CountMissingInstancing(layer.palette);
                if (missing > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"{missing} material(s) on palette prefabs have GPU Instancing DISABLED — their instances are invisible until fixed.",
                        MessageType.Error);
                    if (GUILayout.Button($"Enable GPU Instancing on {missing} material(s)"))
                    {
                        ScatterMaterialUtility.EnableInstancing(layer.palette);
                        layer.MarkPaletteDirty();
                        layer.MarkDirty();
                        SceneView.RepaintAll();
                    }
                }
            }
            return canPaint;
        }

        private bool DrawSculptGUI()
        {
            // Raise/Lower share one entry: Ctrl inverts while painting
            int opIndex = sculptOp == SculptOp.Smooth ? 1
                : sculptOp == SculptOp.Flatten ? 2
                : sculptOp == SculptOp.Stamp ? 3 : 0;
            opIndex = GUILayout.Toolbar(opIndex, new[] { "Raise/Lower", "Smooth", "Flatten", "Stamp" });
            sculptOp = opIndex == 1 ? SculptOp.Smooth
                : opIndex == 2 ? SculptOp.Flatten
                : opIndex == 3 ? SculptOp.Stamp : SculptOp.Raise;
            sculptStrength = EditorGUILayout.Slider("Strength", sculptStrength, 0f, 1f);
            if (sculptOp == SculptOp.Stamp)
            {
                stampTexture = (Texture2D)EditorGUILayout.ObjectField("Stamp Heightmap", stampTexture, typeof(Texture2D), false);
                if (stampTexture == null)
                    sculptHardness = EditorGUILayout.Slider("Hardness (round stamp)", sculptHardness, 0f, 1f);
                stampHeight = EditorGUILayout.FloatField("Stamp Height (m)", stampHeight);
                stampAdditive = EditorGUILayout.Toggle("Additive Blend", stampAdditive);
                stampRandomRotation = EditorGUILayout.Toggle("Random Rotation", stampRandomRotation);
                if (!stampRandomRotation)
                    stampRotation = EditorGUILayout.Slider("Rotation", stampRotation, 0f, 360f);
                EditorGUILayout.HelpBox(
                    "Click to stamp (no drag). Works on terrains and meshes. On meshes the stamp displaces along the click normal; custom stamp textures need Read/Write enabled there.",
                    MessageType.None);
            }
            else
            {
                sculptHardness = EditorGUILayout.Slider("Hardness", sculptHardness, 0f, 1f);
                EditorGUILayout.HelpBox(
                    "Works on terrains and meshes (a collider is required to paint; MeshCollider re-cooks at stroke end). Ctrl inverts Raise/Lower. Flatten targets the point first clicked.",
                    MessageType.None);
            }
            return true;
        }

        private bool DrawTextureGUI()
        {
            terrainLayer = (TerrainLayer)EditorGUILayout.ObjectField("Terrain Layer", terrainLayer, typeof(TerrainLayer), false);
            sculptStrength = EditorGUILayout.Slider("Opacity", sculptStrength, 0f, 1f);
            sculptHardness = EditorGUILayout.Slider("Hardness", sculptHardness, 0f, 1f);
            if (terrainLayer == null)
            {
                EditorGUILayout.HelpBox("Assign a Terrain Layer asset to paint with.", MessageType.Warning);
                return false;
            }
            EditorGUILayout.HelpBox("Terrain only. The layer is auto-added to painted terrains if missing.", MessageType.None);
            return true;
        }

        private bool DrawGrassGUI()
        {
            grassPrefab = (GameObject)EditorGUILayout.ObjectField("Grass Mesh Prefab", grassPrefab, typeof(GameObject), false);
            grassTexture = (Texture2D)EditorGUILayout.ObjectField("Grass Texture", grassTexture, typeof(Texture2D), false);
            grassDensity = EditorGUILayout.IntSlider("Density", grassDensity, 1, 15);
            sculptStrength = EditorGUILayout.Slider("Strength", sculptStrength, 0f, 1f);
            sculptHardness = EditorGUILayout.Slider("Hardness", sculptHardness, 0f, 1f);
            if (grassPrefab == null && grassTexture == null)
            {
                EditorGUILayout.HelpBox("Assign a grass mesh prefab or a billboard texture.", MessageType.Warning);
                return false;
            }
            if (grassPrefab != null &&
                (grassPrefab.GetComponent<MeshFilter>() == null || grassPrefab.GetComponentInChildren<LODGroup>() != null))
            {
                EditorGUILayout.HelpBox(
                    "This prefab won't render as a terrain detail: details need a simple mesh (MeshFilter on the root, no LODGroup). Trees and props belong in the Scatter tab.",
                    MessageType.Error);
            }
            EditorGUILayout.HelpBox(
                "Paints on UNITY TERRAINS only (native details: millions of blades). The cursor disc turns gray on invalid surfaces. Prefab wins over texture. Ctrl erases. For grass on regular meshes use the Scatter tab with a grass prefab.",
                MessageType.None);
            return true;
        }

        private void CreateLayer()
        {
            var go = new GameObject("Scatter Layer", typeof(ScatterLayer));
            Undo.RegisterCreatedObjectUndo(go, "Create Scatter Layer");
            layer = go.GetComponent<ScatterLayer>();
        }

        private void QuickAddToPalette()
        {
            GameObject prefabAsset = quickAddCandidate;
            if (prefabAsset != null && prefabAsset.scene.IsValid())
                prefabAsset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(quickAddCandidate);

            if (prefabAsset == null)
            {
                EditorUtility.DisplayDialog("OmniBrush",
                    $"'{quickAddCandidate.name}' is not a prefab or a prefab instance. The palette can only reference assets — drag the object into the Project window to make it a prefab first.",
                    "OK");
                return;
            }
            if (layer.palette.entries.Exists(e => e.prefab == prefabAsset))
            {
                EditorUtility.DisplayDialog("OmniBrush",
                    $"'{prefabAsset.name}' is already in this palette.", "OK");
                quickAddCandidate = null;
                return;
            }

            Undo.RecordObject(layer.palette, "Add Palette Entry");
            layer.palette.entries.Add(new ScatterPalette.Entry { prefab = prefabAsset });
            EditorUtility.SetDirty(layer.palette);
            layer.MarkPaletteDirty();
            quickAddCandidate = null;

            int missing = ScatterMaterialUtility.CountMissingInstancing(prefabAsset);
            if (missing > 0 && EditorUtility.DisplayDialog("OmniBrush",
                    $"{missing} material(s) on '{prefabAsset.name}' have GPU Instancing disabled — painted instances would be invisible. Enable it now?",
                    "Enable", "Skip"))
            {
                ScatterMaterialUtility.EnableInstancing(prefabAsset);
                layer.MarkPaletteDirty();
                SceneView.RepaintAll();
            }
        }

        // ------------------------------------------------------------ scene GUI

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!paintMode) return;
            if (mode == Mode.Scatter && (layer == null || layer.palette == null)) return;
            if (mode == Mode.Texture && terrainLayer == null) return;
            if (mode == Mode.Grass && grassPrefab == null && grassTexture == null) return;

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
                bool isTerrain = hit.collider is TerrainCollider;
                bool surfaceValid = true;
                string invalidMessage = null;
                if (mode == Mode.Texture || mode == Mode.Grass)
                {
                    surfaceValid = isTerrain;
                    invalidMessage = "Not a Unity Terrain";
                }
                else if (mode == Mode.Sculpt)
                {
                    surfaceValid = isTerrain || hit.collider.GetComponent<MeshFilter>() != null;
                    invalidMessage = "No MeshFilter on this collider";
                }

                bool destructive = (mode == Mode.Scatter || mode == Mode.Grass) && modifier;
                Color color = !surfaceValid ? new Color(0.55f, 0.55f, 0.55f)
                    : destructive ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.55f, 0f);
                Handles.color = color;
                Handles.DrawWireDisc(hit.point, hit.normal, radius);
                Handles.color = new Color(color.r, color.g, color.b, 0.4f);
                float inner = mode == Mode.Scatter ? Mathf.Lerp(1f, 0.5f, falloff) : Mathf.Lerp(0.5f, 1f, sculptHardness);
                Handles.DrawWireDisc(hit.point, hit.normal, radius * inner);
                if (!surfaceValid)
                    Handles.Label(hit.point + hit.normal * (radius * 0.3f), invalidMessage);
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
                        else if (mode == Mode.Texture)
                        {
                            sculptStrokeStarted = false;
                            TextureStamp(hit);
                        }
                        else if (mode == Mode.Grass)
                        {
                            sculptStrokeStarted = false;
                            GrassStamp(hit, modifier);
                        }
                        else
                        {
                            sculptStrokeStarted = false;
                            flattenPoint = hit.point;
                            flattenNormal = hit.normal;
                            currentStampRotation = stampRandomRotation ? Random.Range(0f, 360f) : stampRotation;
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
                        else if (mode == Mode.Texture)
                        {
                            TextureStamp(hit);
                        }
                        else if (mode == Mode.Grass)
                        {
                            GrassStamp(hit, modifier);
                        }
                        else if (sculptOp != SculptOp.Stamp) // stamps are click-only
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
                        if (touchedMeshes.Count > 0)
                        {
                            foreach (MeshFilter mf in touchedMeshes)
                                MeshPaintableSurface.RefreshCollider(mf);
                            touchedMeshes.Clear();
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
            if (surface == null)
            {
                MeshPaintableSurface meshSurface = MeshPaintableSurface.TryFrom(hit.collider);
                if (meshSurface == null) return;
                touchedMeshes.Add(meshSurface.Filter);
                surface = meshSurface;
            }

            if (!sculptStrokeStarted)
            {
                SculptUndo.BeginStroke(SculptUndo.StrokeKind.Heights);
                sculptStrokeStarted = true;
            }

            SculptOp op = sculptOp;
            if (invert && op == SculptOp.Raise) op = SculptOp.Lower;
            else if (invert && op == SculptOp.Lower) op = SculptOp.Raise;

            surface.ApplyStamp(new SculptStampArgs
            {
                op = op,
                center = hit.point,
                brushNormal = hit.normal,
                radius = radius,
                strength = sculptStrength,
                hardness = sculptHardness,
                rotation = op == SculptOp.Stamp ? currentStampRotation : 0f,
                flattenHeight = flattenPoint.y,
                flattenPoint = flattenPoint,
                flattenNormal = flattenNormal,
                stampTexture = op == SculptOp.Stamp ? stampTexture : null,
                stampHeight = stampHeight,
                stampAdditive = stampAdditive,
            });
        }

        private void TextureStamp(RaycastHit hit)
        {
            TerrainPaintableSurface surface = TerrainPaintableSurface.TryFrom(hit.collider);
            if (surface == null) return;

            if (!sculptStrokeStarted)
            {
                SculptUndo.BeginStroke(SculptUndo.StrokeKind.Alphamaps);
                sculptStrokeStarted = true;
            }
            surface.ApplyTexturePaint(hit.point, radius, sculptStrength, sculptHardness, terrainLayer);
        }

        private void GrassStamp(RaycastHit hit, bool erase)
        {
            if (!(hit.collider is TerrainCollider)) return;
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null) return;

            if (!sculptStrokeStarted)
            {
                // kind only routes PaintContext captures; detail records are self-typed
                SculptUndo.BeginStroke(SculptUndo.StrokeKind.Heights);
                sculptStrokeStarted = true;
            }
            int detailLayer = TerrainDetailPainter.EnsurePrototype(terrain, grassTexture, grassPrefab);
            if (detailLayer < 0) return;
            TerrainDetailPainter.PaintDensity(terrain, detailLayer, hit.point, radius,
                sculptStrength, sculptHardness, grassDensity, erase);
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
            int placed = 0, rejFalloff = 0, rejRay = 0, rejSlope = 0, rejHeight = 0, rejDistance = 0, rejWeight = 0;
            for (int i = 0; i < instancesPerStamp; i++)
            {
                Vector2 disc = Random.insideUnitCircle;
                if (falloff > 0f && Random.value < falloff * disc.magnitude) { rejFalloff++; continue; } // edge falloff
                Vector3 candidate = hit.point + (tangent * disc.x + bitangent * disc.y) * radius;

                // Re-project each candidate onto the surface.
                Vector3 origin = candidate + n * radius;
                if (!Physics.Raycast(origin, -n, out RaycastHit surface, radius * 2f, surfaceMask, QueryTriggerInteraction.Ignore))
                {
                    rejRay++;
                    continue;
                }

                float slope = Vector3.Angle(surface.normal, Vector3.up);
                if (slope < slopeMin || slope > slopeMax) { rejSlope++; continue; }
                if (filterHeight && (surface.point.y < heightMin || surface.point.y > heightMax)) { rejHeight++; continue; }
                if (minDistance > 0f && layer.HasInstanceWithin(surface.point, minDistance)) { rejDistance++; continue; }

                int entryIndex = palette.PickWeightedIndex(Random.value);
                if (entryIndex < 0) { rejWeight++; continue; }
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
                placed++;
            }
            if (changed) EditorUtility.SetDirty(layer);

            if (placed == 0)
            {
                var reasons = new System.Collections.Generic.List<string>();
                if (rejHeight > 0) reasons.Add($"{rejHeight} outside height filter ({heightMin:0}–{heightMax:0})");
                if (rejSlope > 0) reasons.Add($"{rejSlope} outside slope filter ({slopeMin:0}–{slopeMax:0}°)");
                if (rejDistance > 0) reasons.Add($"{rejDistance} too close to existing (Min Distance {minDistance:0.#})");
                if (rejFalloff > 0) reasons.Add($"{rejFalloff} rejected by Edge Falloff ({falloff:0.##})");
                if (rejRay > 0) reasons.Add($"{rejRay} missed the surface (Surface Layers mask?)");
                if (rejWeight > 0) reasons.Add($"{rejWeight} found no palette entry with weight > 0");
                lastStampWarning = $"Last stamp placed 0/{instancesPerStamp} instances: " + string.Join(", ", reasons) + ".";
            }
            else
            {
                lastStampWarning = null;
            }
            Repaint();
        }
    }
}
