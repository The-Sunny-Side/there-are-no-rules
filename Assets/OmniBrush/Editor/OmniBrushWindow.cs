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
        private enum Mode { Scatter, Sculpt, Texture, Grass, Spline }

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
        [SerializeField] private bool singlePlace;
        [SerializeField] private int instancesPerStamp = 8;
        [SerializeField] private float strokeSpacing = 0.5f; // fraction of radius
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float falloff = 0.5f;
        [SerializeField] private float slopeMin;
        [SerializeField] private float slopeMax = 45f;
        [SerializeField] private bool filterHeight;
        [SerializeField] private float heightMin;
        [SerializeField] private float heightMax = 1000f;
        [SerializeField] private bool noiseFilter;
        [SerializeField] private float noiseScale = 20f;
        [SerializeField] private float noiseThreshold = 0.5f;
        [SerializeField] private bool layerFilter;
        [SerializeField] private TerrainLayer layerFilterLayer;
        [SerializeField] private float layerFilterMin = 0.5f;
        [SerializeField] private bool curvatureFilter;
        [SerializeField] private bool curvatureConcave = true;
        [SerializeField] private float curvatureSampleDist = 2f;
        [SerializeField] private float curvatureMinDepth = 0.2f;

        // sculpt
        [SerializeField] private SculptOp sculptOp = SculptOp.Raise;
        [SerializeField] private bool procOp;
        [SerializeField] private ProceduralBrush proceduralBrush;
        [SerializeField] private float sculptStrength = 0.5f;
        [SerializeField] private float sculptHardness = 0.5f;

        [SerializeField] private GameObject quickAddCandidate;

        // texture paint
        [SerializeField] private TerrainLayer terrainLayer;
        [SerializeField] private Color meshVertexColor = new Color(0.85f, 0.2f, 0.15f);

        // spline ops
        [SerializeField] private OmniSpline splineTarget;
        [SerializeField] private float splineWidth = 6f;
        [SerializeField] private float splineFeather = 4f;
        [SerializeField] private float splineScatterSpacing = 5f;
        [SerializeField] private int splineScatterSide = 3; // 0 center, 1 left, 2 right, 3 both
        [SerializeField] private float splineScatterOffset = 4f;
        [SerializeField] private float splineScatterJitter = 1f;

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

        [SerializeField] private float dropHeight = 0.5f;
        [SerializeField] private float settleSeconds = 4f;

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
            var newMode = (Mode)GUILayout.Toolbar((int)mode, new[] { "Scatter", "Sculpt", "Texture", "Grass", "Spline" }, GUILayout.Height(24));
            if (newMode != mode)
            {
                mode = newMode;
                SceneView.RepaintAll();
            }
            EditorGUILayout.Space();

            if (mode != Mode.Spline)
            {
                radius = EditorGUILayout.Slider("Radius", radius, 0.1f, 50f);
                int concatenated = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(surfaceMask);
                concatenated = EditorGUILayout.MaskField("Surface Layers", concatenated, InternalEditorUtility.layers);
                surfaceMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(concatenated);
                EditorGUILayout.Space();
            }

            bool canPaint = mode == Mode.Sculpt ? DrawSculptGUI()
                : mode == Mode.Texture ? DrawTextureGUI()
                : mode == Mode.Grass ? DrawGrassGUI()
                : mode == Mode.Spline ? DrawSplineGUI()
                : DrawScatterGUI();

            if (mode == Mode.Spline) return; // spline ops are button-driven, no brush

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
            singlePlace = EditorGUILayout.Toggle(new GUIContent("Single Place (click)",
                "Place exactly one instance per click at the cursor. Filters are bypassed; footprints/Min Distance still apply. Ctrl-drag still erases."), singlePlace);
            using (new EditorGUI.DisabledScope(singlePlace))
                instancesPerStamp = EditorGUILayout.IntSlider("Instances / Stamp", instancesPerStamp, 1, 64);
            strokeSpacing = EditorGUILayout.Slider("Stroke Spacing", strokeSpacing, 0.05f, 2f);
            minDistance = EditorGUILayout.Slider("Min Distance", minDistance, 0f, 20f);
            falloff = EditorGUILayout.Slider("Edge Falloff", falloff, 0f, 1f);

            EditorGUILayout.Space();
            GUILayout.Label("Filters", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Slope (°)");
                slopeMin = EditorGUILayout.FloatField(Mathf.Round(slopeMin), GUILayout.Width(34));
                EditorGUILayout.MinMaxSlider(ref slopeMin, ref slopeMax, 0f, 90f);
                slopeMax = EditorGUILayout.FloatField(Mathf.Round(slopeMax), GUILayout.Width(34));
            }
            slopeMin = Mathf.Clamp(slopeMin, 0f, 90f);
            slopeMax = Mathf.Clamp(slopeMax, slopeMin, 90f);
            filterHeight = EditorGUILayout.Toggle("Filter Height", filterHeight);
            if (filterHeight)
            {
                heightMin = EditorGUILayout.FloatField("Min Y", heightMin);
                heightMax = EditorGUILayout.FloatField("Max Y", heightMax);
            }
            noiseFilter = EditorGUILayout.Toggle("Noise Mask", noiseFilter);
            if (noiseFilter)
            {
                EditorGUI.indentLevel++;
                noiseScale = EditorGUILayout.Slider("Patch Size (m)", noiseScale, 1f, 200f);
                noiseThreshold = EditorGUILayout.Slider("Coverage", 1f - noiseThreshold, 0f, 1f);
                noiseThreshold = 1f - noiseThreshold;
                EditorGUI.indentLevel--;
            }
            layerFilter = EditorGUILayout.Toggle("Only On Terrain Layer", layerFilter);
            if (layerFilter)
            {
                EditorGUI.indentLevel++;
                layerFilterLayer = (TerrainLayer)EditorGUILayout.ObjectField("Layer", layerFilterLayer, typeof(TerrainLayer), false);
                layerFilterMin = EditorGUILayout.Slider("Min Weight", layerFilterMin, 0f, 1f);
                EditorGUI.indentLevel--;
            }
            curvatureFilter = EditorGUILayout.Toggle("Curvature", curvatureFilter);
            if (curvatureFilter)
            {
                EditorGUI.indentLevel++;
                curvatureConcave = GUILayout.Toolbar(curvatureConcave ? 0 : 1, new[] { "Hollows", "Bumps" }) == 0;
                curvatureSampleDist = EditorGUILayout.Slider("Sample Distance", curvatureSampleDist, 0.2f, 10f);
                curvatureMinDepth = EditorGUILayout.Slider("Min Depth", curvatureMinDepth, 0f, 5f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            GUILayout.Label("Physics Drop", EditorStyles.boldLabel);
            dropHeight = EditorGUILayout.Slider("Drop Height", dropHeight, 0f, 5f);
            settleSeconds = EditorGUILayout.Slider("Max Sim Time (s)", settleSeconds, 0.5f, 10f);
            using (new EditorGUI.DisabledScope(layer.Count == 0))
            {
                if (GUILayout.Button($"Drop & Settle {layer.Count} Instances"))
                {
                    if (layer.Count <= 2000 || EditorUtility.DisplayDialog("OmniBrush",
                            $"Simulate physics on {layer.Count} rigidbodies? This can take a while.", "Settle", "Cancel"))
                    {
                        int settled = PhysicsDrop.Settle(layer, dropHeight, settleSeconds);
                        lastStampWarning = $"Physics drop settled {settled} instance(s). One undo step reverts it.";
                    }
                }
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
            int opIndex = procOp ? 4
                : sculptOp == SculptOp.Smooth ? 1
                : sculptOp == SculptOp.Flatten ? 2
                : sculptOp == SculptOp.Stamp ? 3 : 0;
            opIndex = GUILayout.Toolbar(opIndex, new[] { "Raise/Lower", "Smooth", "Flatten", "Stamp", "Proc" });
            procOp = opIndex == 4;
            if (!procOp)
                sculptOp = opIndex == 1 ? SculptOp.Smooth
                    : opIndex == 2 ? SculptOp.Flatten
                    : opIndex == 3 ? SculptOp.Stamp : SculptOp.Raise;
            sculptStrength = EditorGUILayout.Slider("Strength", sculptStrength, 0f, 1f);
            if (procOp)
            {
                proceduralBrush = (ProceduralBrush)EditorGUILayout.ObjectField("Procedural Brush", proceduralBrush, typeof(ProceduralBrush), false);
                if (proceduralBrush == null && GUILayout.Button("New Procedural Brush Asset"))
                {
                    string path = EditorUtility.SaveFilePanelInProject("Create Procedural Brush", "ProceduralBrush", "asset", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var asset = CreateInstance<ProceduralBrush>();
                        AssetDatabase.CreateAsset(asset, path);
                        AssetDatabase.SaveAssets();
                        proceduralBrush = asset;
                    }
                }
                sculptHardness = EditorGUILayout.Slider("Hardness", sculptHardness, 0f, 1f);
                EditorGUILayout.HelpBox(
                    "Paints the brush asset's layer stack (noise/constant, add/multiply/min/max) as height. Edit layers in the asset inspector. Terrain only for now. Ctrl inverts (digs).",
                    MessageType.None);
                return proceduralBrush != null;
            }
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
            GUILayout.Label("On Terrains — splat", EditorStyles.boldLabel);
            terrainLayer = (TerrainLayer)EditorGUILayout.ObjectField("Terrain Layer", terrainLayer, typeof(TerrainLayer), false);
            GUILayout.Label("On Meshes — vertex color", EditorStyles.boldLabel);
            meshVertexColor = EditorGUILayout.ColorField("Vertex Color", meshVertexColor);

            sculptStrength = EditorGUILayout.Slider("Opacity", sculptStrength, 0f, 1f);
            sculptHardness = EditorGUILayout.Slider("Hardness", sculptHardness, 0f, 1f);

            if (terrainLayer == null)
                EditorGUILayout.HelpBox("No Terrain Layer assigned — terrains will be skipped.", MessageType.Info);
            EditorGUILayout.HelpBox(
                "Meshes get vertex colors (non-destructive clone, undoable). The material must read vertex color to SHOW it — most don't. Select the painted object and use the button below to swap in a vertex-color-aware copy of its material.",
                MessageType.None);
            using (new EditorGUI.DisabledScope(Selection.activeGameObject == null))
            {
                if (GUILayout.Button("Make Selected Object's Material Show Vertex Colors"))
                    MakeSelectionVertexColorVisible();
            }
            return true;
        }

        private static void MakeSelectionVertexColorVisible()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Lit");
            if (shader == null) shader = Shader.Find("Particles/Standard Surface");
            if (shader == null)
            {
                EditorUtility.DisplayDialog("OmniBrush", "No vertex-color-capable builtin shader found in this project.", "OK");
                return;
            }
            int swapped = 0;
            foreach (MeshRenderer renderer in go.GetComponentsInChildren<MeshRenderer>())
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || source.shader == shader) continue;
                    var replacement = new Material(shader) { name = source.name + " (VertexColor)" };
                    if (source.HasProperty("_BaseMap") && replacement.HasProperty("_BaseMap"))
                        replacement.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                    if (source.HasProperty("_BaseColor") && replacement.HasProperty("_BaseColor"))
                        replacement.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                    materials[i] = replacement;
                    swapped++;
                }
                if (swapped > 0)
                {
                    Undo.RecordObject(renderer, "Vertex Color Material");
                    renderer.sharedMaterials = materials;
                }
            }
            Debug.Log($"[OmniBrush] Swapped {swapped} material(s) on '{go.name}' to a vertex-color-aware shader (scene-local copies; originals untouched).");
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
            if (!string.IsNullOrEmpty(lastStampWarning))
                EditorGUILayout.HelpBox(lastStampWarning, MessageType.Warning);
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

        // --------------------------------------------------------------- spline

        private bool DrawSplineGUI()
        {
            splineTarget = (OmniSpline)EditorGUILayout.ObjectField("Spline", splineTarget, typeof(OmniSpline), true);
            if (GUILayout.Button("Create Spline At View Pivot"))
            {
                Vector3 pivot = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
                var go = new GameObject("OmniSpline", typeof(OmniSpline));
                go.transform.position = pivot;
                Undo.RegisterCreatedObjectUndo(go, "Create Spline");
                splineTarget = go.GetComponent<OmniSpline>();
                Selection.activeGameObject = go;
            }
            if (splineTarget == null)
            {
                EditorGUILayout.HelpBox("Create or assign a spline, then select it in the scene to move/add points.", MessageType.Info);
                return false;
            }

            EditorGUILayout.Space();
            GUILayout.Label("Road / River Bed (terrain)", EditorStyles.boldLabel);
            splineWidth = EditorGUILayout.Slider("Width", splineWidth, 0.5f, 30f);
            splineFeather = EditorGUILayout.Slider("Feather", splineFeather, 0f, 20f);
            if (GUILayout.Button("Flatten Terrain Along Spline")) FlattenAlongSpline();

            terrainLayer = (TerrainLayer)EditorGUILayout.ObjectField("Terrain Layer", terrainLayer, typeof(TerrainLayer), false);
            using (new EditorGUI.DisabledScope(terrainLayer == null))
            {
                if (GUILayout.Button("Paint Texture Along Spline")) TextureAlongSpline();
            }

            EditorGUILayout.Space();
            GUILayout.Label("Scatter Along (uses Scatter tab layer + palette)", EditorStyles.boldLabel);
            splineScatterSpacing = EditorGUILayout.Slider("Spacing", splineScatterSpacing, 0.5f, 50f);
            splineScatterSide = GUILayout.Toolbar(splineScatterSide, new[] { "Center", "Left", "Right", "Both" });
            if (splineScatterSide != 0)
                splineScatterOffset = EditorGUILayout.Slider("Side Offset", splineScatterOffset, 0f, 30f);
            splineScatterJitter = EditorGUILayout.Slider("Jitter", splineScatterJitter, 0f, 10f);
            bool scatterReady = layer != null && layer.palette != null && layer.palette.entries.Count > 0;
            if (!scatterReady)
                EditorGUILayout.HelpBox("Set up Layer + Palette in the Scatter tab first.", MessageType.Warning);
            using (new EditorGUI.DisabledScope(!scatterReady))
            {
                if (GUILayout.Button("Scatter Along Spline")) ScatterAlongSpline();
            }

            if (!string.IsNullOrEmpty(lastStampWarning))
                EditorGUILayout.HelpBox(lastStampWarning, MessageType.Info);
            EditorGUILayout.HelpBox(
                "Each button applies once along the whole spline as a single undo step. Terrain ops need Unity Terrains under the path.",
                MessageType.None);
            return false;
        }

        private void FlattenAlongSpline()
        {
            var samples = splineTarget.SampleByDistance(Mathf.Max(0.5f, splineWidth * 0.25f));
            int hit = 0;
            SculptUndo.BeginStroke(SculptUndo.StrokeKind.Heights);
            try
            {
                foreach (Vector3 sample in samples)
                {
                    Terrain terrain = SplineOps.FindTerrainAt(sample);
                    if (terrain == null) continue;
                    SplineOps.FlattenStamp(terrain, sample, splineWidth * 0.5f, splineFeather, sample.y);
                    hit++;
                }
            }
            finally { SculptUndo.EndStroke(); }
            lastStampWarning = hit == 0
                ? "No Unity Terrain under the spline — nothing flattened."
                : $"Flattened terrain under {hit}/{samples.Count} spline samples.";
            SceneView.RepaintAll();
        }

        private void TextureAlongSpline()
        {
            var samples = splineTarget.SampleByDistance(Mathf.Max(0.5f, splineWidth * 0.25f));
            float reach = splineWidth * 0.5f + splineFeather;
            float hardness = reach > 0f ? splineWidth * 0.5f / reach : 1f;
            int hit = 0;
            SculptUndo.BeginStroke(SculptUndo.StrokeKind.Alphamaps);
            try
            {
                foreach (Vector3 sample in samples)
                {
                    Terrain terrain = SplineOps.FindTerrainAt(sample);
                    if (terrain == null) continue;
                    new TerrainPaintableSurface(terrain).ApplyTexturePaint(sample, reach, 1f, hardness, terrainLayer);
                    hit++;
                }
            }
            finally { SculptUndo.EndStroke(); }
            lastStampWarning = hit == 0
                ? "No Unity Terrain under the spline — nothing painted."
                : $"Painted texture under {hit}/{samples.Count} spline samples.";
            SceneView.RepaintAll();
        }

        private void ScatterAlongSpline()
        {
            ScatterPalette palette = layer.palette;
            var samples = splineTarget.SampleByDistance(Mathf.Max(0.5f, splineScatterSpacing));
            float[] sides = splineScatterSide == 0 ? new[] { 0f }
                : splineScatterSide == 1 ? new[] { splineScatterOffset }
                : splineScatterSide == 2 ? new[] { -splineScatterOffset }
                : new[] { splineScatterOffset, -splineScatterOffset };

            Undo.RegisterCompleteObjectUndo(layer, "OmniBrush Spline Scatter");
            int placed = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                Vector3 tangent = (samples[Mathf.Min(i + 1, samples.Count - 1)] - samples[Mathf.Max(i - 1, 0)]).normalized;
                Vector3 sideDir = Vector3.Cross(Vector3.up, tangent).normalized;
                foreach (float side in sides)
                {
                    Vector2 jitter = Random.insideUnitCircle * splineScatterJitter;
                    Vector3 candidate = samples[i] + sideDir * side + new Vector3(jitter.x, 0f, jitter.y);
                    if (!Physics.Raycast(candidate + Vector3.up * 30f, Vector3.down, out RaycastHit surface, 200f, surfaceMask, QueryTriggerInteraction.Ignore))
                        continue;

                    int entryIndex = palette.PickWeightedIndex(Random.value);
                    if (entryIndex < 0) continue;
                    ScatterPalette.Entry entry = palette.entries[entryIndex];
                    float scale = Random.Range(entry.uniformScale.x, entry.uniformScale.y);
                    float footprint = entry.footprintRadius * scale;
                    if ((minDistance > 0f || footprint > 0f) &&
                        layer.OverlapsExisting(surface.point, footprint, minDistance, palette))
                        continue;

                    Quaternion align = Quaternion.Slerp(Quaternion.identity,
                        Quaternion.FromToRotation(Vector3.up, surface.normal), entry.alignToNormal);
                    float yaw = entry.randomYaw ? Random.Range(0f, 360f) : 0f;
                    Quaternion rotation = align * Quaternion.Euler(0f, yaw, 0f);
                    layer.AddInstance(new ScatterInstance
                    {
                        entryIndex = entryIndex,
                        position = surface.point + rotation * Vector3.up * entry.verticalOffset,
                        rotation = rotation,
                        scale = new Vector3(scale, scale, scale),
                    });
                    placed++;
                }
            }
            EditorUtility.SetDirty(layer);
            lastStampWarning = $"Scattered {placed} instance(s) along the spline.";
            SceneView.RepaintAll();
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
            if (!paintMode || mode == Mode.Spline) return;
            if (mode == Mode.Scatter && (layer == null || layer.palette == null)) return;
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
                if (mode == Mode.Grass)
                {
                    surfaceValid = isTerrain;
                    invalidMessage = "Not a Unity Terrain";
                }
                else if (mode == Mode.Sculpt && procOp)
                {
                    surfaceValid = isTerrain;
                    invalidMessage = "Not a Unity Terrain";
                }
                else if (mode == Mode.Sculpt || mode == Mode.Texture)
                {
                    surfaceValid = isTerrain || hit.collider.GetComponent<MeshFilter>() != null;
                    invalidMessage = "No MeshFilter on this collider";
                }
                if (mode == Mode.Texture && isTerrain && terrainLayer == null)
                {
                    surfaceValid = false;
                    invalidMessage = "Assign a Terrain Layer to splat terrains";
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
                            bool dragAllowed = !singlePlace || modifier; // single place is click-only, erase-drag still works
                            if (dragAllowed && (!hasLastStamp || Vector3.Distance(hit.point, lastStampPos) >= Mathf.Max(0.05f, radius * strokeSpacing)))
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
                        else if (procOp || sculptOp != SculptOp.Stamp) // stamps are click-only
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

        private void PlaceSingle(RaycastHit hit)
        {
            ScatterPalette palette = layer.palette;
            int entryIndex = palette.PickWeightedIndex(Random.value);
            if (entryIndex < 0) return;
            ScatterPalette.Entry entry = palette.entries[entryIndex];
            float scale = Random.Range(entry.uniformScale.x, entry.uniformScale.y);
            float candidateFootprint = entry.footprintRadius * scale;
            if ((minDistance > 0f || candidateFootprint > 0f) &&
                layer.OverlapsExisting(hit.point, candidateFootprint, minDistance, palette))
            {
                lastStampWarning = "Single place blocked: too close to an existing instance (footprints / Min Distance).";
                Repaint();
                return;
            }

            Quaternion align = Quaternion.Slerp(Quaternion.identity,
                Quaternion.FromToRotation(Vector3.up, hit.normal), entry.alignToNormal);
            float yaw = entry.randomYaw ? Random.Range(0f, 360f) : 0f;
            Quaternion rotation = align * Quaternion.Euler(0f, yaw, 0f);
            layer.AddInstance(new ScatterInstance
            {
                entryIndex = entryIndex,
                position = hit.point + rotation * Vector3.up * entry.verticalOffset,
                rotation = rotation,
                scale = new Vector3(scale, scale, scale),
            });
            EditorUtility.SetDirty(layer);
            lastStampWarning = null;
            Repaint();
        }

        // --------------------------------------------------------------- sculpt

        private void SculptStamp(RaycastHit hit, bool invert)
        {
            if (procOp)
            {
                if (proceduralBrush == null || !(hit.collider is TerrainCollider)) return;
                Terrain terrain = hit.collider.GetComponent<Terrain>();
                if (terrain == null || terrain.terrainData == null) return;
                if (!sculptStrokeStarted)
                {
                    SculptUndo.BeginStroke(SculptUndo.StrokeKind.Heights);
                    sculptStrokeStarted = true;
                }
                ProceduralOps.Stamp(terrain, proceduralBrush, hit.point, radius, sculptStrength, sculptHardness, invert);
                return;
            }

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
            if (surface != null)
            {
                if (terrainLayer == null) return;
                if (!sculptStrokeStarted)
                {
                    SculptUndo.BeginStroke(SculptUndo.StrokeKind.Alphamaps);
                    sculptStrokeStarted = true;
                }
                surface.ApplyTexturePaint(hit.point, radius, sculptStrength, sculptHardness, terrainLayer);
                return;
            }

            MeshPaintableSurface meshSurface = MeshPaintableSurface.TryFrom(hit.collider);
            if (meshSurface == null) return;
            if (!sculptStrokeStarted)
            {
                SculptUndo.BeginStroke(SculptUndo.StrokeKind.Alphamaps); // kind only routes terrain captures
                sculptStrokeStarted = true;
            }
            meshSurface.ApplyVertexColor(hit.point, hit.normal, radius, sculptStrength, sculptHardness, meshVertexColor);
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
            bool changed = TerrainDetailPainter.PaintDensity(terrain, detailLayer, hit.point, radius,
                sculptStrength, sculptHardness, grassDensity, erase);
            lastStampWarning = changed ? null
                : erase ? "Last stamp: nothing to erase here."
                : "Last stamp changed 0 cells (already at target density here).";
            Repaint();
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
            if (singlePlace)
            {
                PlaceSingle(hit);
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
            int rejNoise = 0, rejLayer = 0, rejCurvature = 0;
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
                if (noiseFilter && !BrushFilters.PassesNoise(surface.point, noiseScale, noiseThreshold)) { rejNoise++; continue; }
                if (layerFilter && layerFilterLayer != null)
                {
                    float weight = BrushFilters.SampleLayerWeight(surface, layerFilterLayer);
                    if (weight >= 0f && weight < layerFilterMin) { rejLayer++; continue; } // non-terrain hits pass
                }
                if (curvatureFilter)
                {
                    float relative = BrushFilters.SampleRelativeHeight(surface.point, surface.normal, curvatureSampleDist, surfaceMask);
                    bool ok = curvatureConcave ? relative >= curvatureMinDepth : relative <= -curvatureMinDepth;
                    if (!ok) { rejCurvature++; continue; }
                }
                int entryIndex = palette.PickWeightedIndex(Random.value);
                if (entryIndex < 0) { rejWeight++; continue; }
                ScatterPalette.Entry entry = palette.entries[entryIndex];
                float scale = Random.Range(entry.uniformScale.x, entry.uniformScale.y);
                float candidateFootprint = entry.footprintRadius * scale;
                if ((minDistance > 0f || candidateFootprint > 0f) &&
                    layer.OverlapsExisting(surface.point, candidateFootprint, minDistance, palette))
                { rejDistance++; continue; }

                Quaternion align = Quaternion.Slerp(Quaternion.identity,
                    Quaternion.FromToRotation(Vector3.up, surface.normal), entry.alignToNormal);
                float yaw = entry.randomYaw ? Random.Range(0f, 360f) : 0f;
                Quaternion rotation = align * Quaternion.Euler(0f, yaw, 0f);
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
                if (rejNoise > 0) reasons.Add($"{rejNoise} masked by noise");
                if (rejLayer > 0) reasons.Add($"{rejLayer} not on terrain layer '{(layerFilterLayer ? layerFilterLayer.name : "?")}'");
                if (rejCurvature > 0) reasons.Add($"{rejCurvature} failed curvature ({(curvatureConcave ? "hollows" : "bumps")})");
                if (rejDistance > 0) reasons.Add($"{rejDistance} too close to existing (footprints / Min Distance {minDistance:0.#})");
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
