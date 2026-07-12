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
        [SerializeField] private float splineBedNoise;
        [SerializeField] private float splineBedNoiseScale = 8f;
        [SerializeField] private float splineEdgeNoise;
        [SerializeField] private float splineEdgeNoiseScale = 12f;
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
            var tabs = new[]
            {
                new GUIContent("Scatter", "Paint prefabs as GPU-instanced data on any collider. Ctrl erases."),
                new GUIContent("Sculpt", "Deform terrains and meshes: raise/lower, smooth, flatten, stamp, procedural."),
                new GUIContent("Texture", "Splat TerrainLayers on terrains, vertex colors on meshes."),
                new GUIContent("Grass", "Paint native terrain detail density (millions of blades)."),
                new GUIContent("Spline", "Carve, texture and scatter along a path (roads, rivers)."),
            };
            var newMode = (Mode)GUILayout.Toolbar((int)mode, tabs, GUILayout.Height(24));
            if (newMode != mode)
            {
                mode = newMode;
                SceneView.RepaintAll();
            }
            EditorGUILayout.Space();

            if (mode != Mode.Spline)
            {
                radius = EditorGUILayout.Slider(new GUIContent("Radius", "Brush radius in world meters."), radius, 0.1f, 150f);
                int concatenated = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(surfaceMask);
                concatenated = EditorGUILayout.MaskField(new GUIContent("Surface Layers",
                    "Physics layers the brush can hit. Colliders on other layers are ignored."), concatenated, InternalEditorUtility.layers);
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

            layer = (ScatterLayer)EditorGUILayout.ObjectField(new GUIContent("Scatter Layer (scene)",
                "Scene object holding the painted instances as pure data — no GameObjects, GPU-instanced rendering. Bake when you need colliders."),
                layer, typeof(ScatterLayer), true);
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
            var palette = (ScatterPalette)EditorGUILayout.ObjectField(new GUIContent("Palette (asset)",
                "Asset listing what to paint: prefabs with pick weight, scale range, alignment, footprint."),
                layer.palette, typeof(ScatterPalette), false);
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
                    quickAddCandidate = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Quick Add Prefab",
                        "Drop a prefab (or a scene instance of one) and press Add to append it to the palette."),
                        quickAddCandidate, typeof(GameObject), true);
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
                instancesPerStamp = EditorGUILayout.IntSlider(new GUIContent("Instances / Stamp",
                    "Placement candidates tried per stamp. Rejected ones (filters, spacing) are reported below."), instancesPerStamp, 1, 64);
            strokeSpacing = EditorGUILayout.Slider(new GUIContent("Stroke Spacing",
                "Distance between stamps while dragging, as a fraction of Radius."), strokeSpacing, 0.05f, 2f);
            minDistance = EditorGUILayout.Slider(new GUIContent("Min Distance",
                "Global minimum distance between any two instances. Per-prefab Footprint Radius from the palette adds on top."), minDistance, 0f, 20f);
            falloff = EditorGUILayout.Slider(new GUIContent("Edge Falloff",
                "Rejects more candidates toward the brush edge, feathering the patch border."), falloff, 0f, 1f);

            EditorGUILayout.Space();
            GUILayout.Label("Filters", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(new GUIContent("Slope (°)",
                    "Accept candidates only where the surface slope is inside this range (0 = flat, 90 = wall)."));
                slopeMin = EditorGUILayout.FloatField(Mathf.Round(slopeMin), GUILayout.Width(34));
                EditorGUILayout.MinMaxSlider(ref slopeMin, ref slopeMax, 0f, 90f);
                slopeMax = EditorGUILayout.FloatField(Mathf.Round(slopeMax), GUILayout.Width(34));
            }
            slopeMin = Mathf.Clamp(slopeMin, 0f, 90f);
            slopeMax = Mathf.Clamp(slopeMax, slopeMin, 90f);
            filterHeight = EditorGUILayout.Toggle(new GUIContent("Filter Height",
                "Accept candidates only between Min Y and Max Y (world height)."), filterHeight);
            if (filterHeight)
            {
                heightMin = EditorGUILayout.FloatField(new GUIContent("Min Y", "Lowest accepted world height."), heightMin);
                heightMax = EditorGUILayout.FloatField(new GUIContent("Max Y", "Highest accepted world height."), heightMax);
            }
            noiseFilter = EditorGUILayout.Toggle(new GUIContent("Noise Mask",
                "Paint only inside procedural Perlin patches. Deterministic: repainting the area gives the same patches."), noiseFilter);
            if (noiseFilter)
            {
                EditorGUI.indentLevel++;
                noiseScale = EditorGUILayout.Slider(new GUIContent("Patch Size (m)",
                    "Approximate size of the noise patches in meters."), noiseScale, 1f, 200f);
                noiseThreshold = EditorGUILayout.Slider(new GUIContent("Coverage",
                    "Fraction of the area the patches cover: low = sparse islands, high = almost everywhere."), 1f - noiseThreshold, 0f, 1f);
                noiseThreshold = 1f - noiseThreshold;
                EditorGUI.indentLevel--;
            }
            layerFilter = EditorGUILayout.Toggle(new GUIContent("Only On Terrain Layer",
                "Paint only where a given splat texture dominates the terrain. Non-terrain surfaces pass through."), layerFilter);
            if (layerFilter)
            {
                EditorGUI.indentLevel++;
                layerFilterLayer = (TerrainLayer)EditorGUILayout.ObjectField(new GUIContent("Layer",
                    "The TerrainLayer that must be present under each candidate."), layerFilterLayer, typeof(TerrainLayer), false);
                layerFilterMin = EditorGUILayout.Slider(new GUIContent("Min Weight",
                    "Minimum splat weight (0-1) of the layer for a candidate to pass."), layerFilterMin, 0f, 1f);
                EditorGUI.indentLevel--;
            }
            curvatureFilter = EditorGUILayout.Toggle(new GUIContent("Curvature",
                "Paint only in hollows (pits, valleys) or on bumps (ridges, mounds), probing around each candidate."), curvatureFilter);
            if (curvatureFilter)
            {
                EditorGUI.indentLevel++;
                curvatureConcave = GUILayout.Toolbar(curvatureConcave ? 0 : 1, new[]
                {
                    new GUIContent("Hollows", "Keep candidates whose neighbors sit higher (pits, gullies)."),
                    new GUIContent("Bumps", "Keep candidates whose neighbors sit lower (ridges, mounds)."),
                }) == 0;
                curvatureSampleDist = EditorGUILayout.Slider(new GUIContent("Sample Distance",
                    "How far around each candidate the surface is probed, in meters."), curvatureSampleDist, 0.2f, 10f);
                curvatureMinDepth = EditorGUILayout.Slider(new GUIContent("Min Depth",
                    "Minimum height difference for a hollow/bump to qualify, in meters."), curvatureMinDepth, 0f, 5f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            GUILayout.Label("Physics Drop", EditorStyles.boldLabel);
            dropHeight = EditorGUILayout.Slider(new GUIContent("Drop Height",
                "Lift each instance this many meters before dropping it."), dropHeight, 0f, 5f);
            settleSeconds = EditorGUILayout.Slider(new GUIContent("Max Sim Time (s)",
                "Physics simulation budget. Ends early once every body is asleep."), settleSeconds, 0.5f, 10f);
            using (new EditorGUI.DisabledScope(layer.Count == 0))
            {
                if (GUILayout.Button(new GUIContent($"Drop & Settle {layer.Count} Instances",
                    "In-editor physics: instances fall, roll against scene colliders and each other, and their settled transforms are written back. One undo step.")))
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
            opIndex = GUILayout.Toolbar(opIndex, new[]
            {
                new GUIContent("Raise/Lower", "Push the surface up along its normal. Hold Ctrl to pull down."),
                new GUIContent("Smooth", "Relax bumps and spikes toward the local plane."),
                new GUIContent("Flatten", "Level the surface toward the point first clicked."),
                new GUIContent("Stamp", "Press a heightmap shape once per click (mountains, craters)."),
                new GUIContent("Proc", "Paint a procedural layer stack (noise, constants) as height."),
            });
            procOp = opIndex == 4;
            if (!procOp)
                sculptOp = opIndex == 1 ? SculptOp.Smooth
                    : opIndex == 2 ? SculptOp.Flatten
                    : opIndex == 3 ? SculptOp.Stamp : SculptOp.Raise;
            sculptStrength = EditorGUILayout.Slider(new GUIContent("Strength",
                "How much effect each stamp applies. Drag strokes stamp continuously."), sculptStrength, 0f, 1f);
            if (procOp)
            {
                proceduralBrush = (ProceduralBrush)EditorGUILayout.ObjectField(new GUIContent("Procedural Brush",
                    "Layer-stack asset (noise/constant, blend modes) evaluated per world position. Edit layers in its inspector."),
                    proceduralBrush, typeof(ProceduralBrush), false);
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
                sculptHardness = EditorGUILayout.Slider(new GUIContent("Hardness",
                    "Inner fraction of the brush at full effect before the falloff begins."), sculptHardness, 0f, 1f);
                EditorGUILayout.HelpBox(
                    "Paints the brush asset's layer stack (noise/constant, add/multiply/min/max) as height on terrains, or as displacement along the click normal on meshes. Edit layers in the asset inspector. Ctrl inverts (digs).",
                    MessageType.None);
                return proceduralBrush != null;
            }
            if (sculptOp == SculptOp.Stamp)
            {
                stampTexture = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("Stamp Heightmap",
                    "Grayscale texture used as the stamp shape (red channel). Empty = round procedural stamp."),
                    stampTexture, typeof(Texture2D), false);
                if (stampTexture == null)
                    sculptHardness = EditorGUILayout.Slider(new GUIContent("Hardness (round stamp)",
                        "Falloff hardness of the procedural round stamp."), sculptHardness, 0f, 1f);
                stampHeight = EditorGUILayout.FloatField(new GUIContent("Stamp Height (m)",
                    "Peak height of the stamp in meters."), stampHeight);
                stampAdditive = EditorGUILayout.Toggle(new GUIContent("Additive Blend",
                    "Add on top of the existing ground instead of raising it up to the shape (max, idempotent)."), stampAdditive);
                stampRandomRotation = EditorGUILayout.Toggle(new GUIContent("Random Rotation",
                    "Rotate each stamp randomly for natural variety."), stampRandomRotation);
                if (!stampRandomRotation)
                    stampRotation = EditorGUILayout.Slider(new GUIContent("Rotation",
                        "Fixed stamp rotation in degrees."), stampRotation, 0f, 360f);
                EditorGUILayout.HelpBox(
                    "Click to stamp (no drag). Works on terrains and meshes. On meshes the stamp displaces along the click normal; custom stamp textures need Read/Write enabled there.",
                    MessageType.None);
            }
            else
            {
                sculptHardness = EditorGUILayout.Slider(new GUIContent("Hardness",
                    "Inner fraction of the brush at full effect before the falloff begins."), sculptHardness, 0f, 1f);
                EditorGUILayout.HelpBox(
                    "Works on terrains and meshes (a collider is required to paint; MeshCollider re-cooks at stroke end). Ctrl inverts Raise/Lower. Flatten targets the point first clicked.",
                    MessageType.None);
            }
            return true;
        }

        private bool DrawTextureGUI()
        {
            GUILayout.Label("On Terrains — splat", EditorStyles.boldLabel);
            terrainLayer = (TerrainLayer)EditorGUILayout.ObjectField(new GUIContent("Terrain Layer",
                "Splat layer painted on terrains. Auto-added to the terrain when missing."),
                terrainLayer, typeof(TerrainLayer), false);
            GUILayout.Label("On Meshes — vertex color", EditorStyles.boldLabel);
            meshVertexColor = EditorGUILayout.ColorField(new GUIContent("Vertex Color",
                "Color painted into the mesh's vertex colors. The material must read COLOR0 to show it — see the button below."),
                meshVertexColor);

            sculptStrength = EditorGUILayout.Slider(new GUIContent("Opacity",
                "Blend amount toward the target per stamp."), sculptStrength, 0f, 1f);
            sculptHardness = EditorGUILayout.Slider(new GUIContent("Hardness",
                "Inner fraction of the brush at full effect before the falloff begins."), sculptHardness, 0f, 1f);

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
            grassPrefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Grass Mesh Prefab",
                "Simple mesh rendered as a terrain detail: MeshFilter on the root, no LODGroup. Wins over the texture. Trees belong in Scatter."),
                grassPrefab, typeof(GameObject), false);
            grassTexture = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("Grass Texture",
                "Billboard texture rendered as grass blades by the terrain."),
                grassTexture, typeof(Texture2D), false);
            grassDensity = EditorGUILayout.IntSlider(new GUIContent("Density",
                "Target grass density under the brush. Auto-scaled to the terrain's detail scatter mode."), grassDensity, 1, 15);
            sculptStrength = EditorGUILayout.Slider(new GUIContent("Strength",
                "How fast density builds toward the target per stamp."), sculptStrength, 0f, 1f);
            sculptHardness = EditorGUILayout.Slider(new GUIContent("Hardness",
                "Inner fraction of the brush at full effect before the falloff begins."), sculptHardness, 0f, 1f);
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
            splineTarget = (OmniSpline)EditorGUILayout.ObjectField(new GUIContent("Spline",
                "Scene spline to operate along. Select it in the scene to move points; Shift+Click there appends points."),
                splineTarget, typeof(OmniSpline), true);
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
            splineWidth = EditorGUILayout.Slider(new GUIContent("Width",
                "Full-effect corridor width in meters (the flat road bed)."), splineWidth, 0.5f, 100f);
            splineFeather = EditorGUILayout.Slider(new GUIContent("Feather",
                "Blend margin outside the width, easing into the surroundings."), splineFeather, 0f, 60f);
            splineBedNoise = EditorGUILayout.Slider(new GUIContent("Bed Noise (m)",
                "Perlin height wobble added to the flattened bed — riverbeds, dirt roads. 0 = perfectly flat."), splineBedNoise, 0f, 10f);
            if (splineBedNoise > 0f)
                splineBedNoiseScale = EditorGUILayout.Slider(new GUIContent("Bed Noise Scale (m)",
                    "Feature size of the bed wobble — small = ripples, large = gentle undulation."), splineBedNoiseScale, 1f, 100f);
            splineEdgeNoise = EditorGUILayout.Slider(new GUIContent("Edge Noise (m)",
                "Perlin wobble of the corridor borders so the edges aren't straight lines."), splineEdgeNoise, 0f, 15f);
            if (splineEdgeNoise > 0f)
                splineEdgeNoiseScale = EditorGUILayout.Slider(new GUIContent("Edge Noise Scale (m)",
                    "Feature size of the border wobble along the path."), splineEdgeNoiseScale, 1f, 100f);
            if (GUILayout.Button(new GUIContent("Flatten Terrain Along Spline",
                "Level the ground to the spline's height along the whole path — terrains and meshes. One undo step.")))
                FlattenAlongSpline();

            terrainLayer = (TerrainLayer)EditorGUILayout.ObjectField(
                new GUIContent("Terrain Layer", "Splat painted on terrains along the path."),
                terrainLayer, typeof(TerrainLayer), false);
            meshVertexColor = EditorGUILayout.ColorField(
                new GUIContent("Mesh Vertex Color", "Vertex color painted on meshes along the path (material must read COLOR0)."),
                meshVertexColor);
            if (GUILayout.Button(new GUIContent("Paint Texture Along Spline",
                "Terrains get the Terrain Layer splat, meshes get the vertex color — one undo step.")))
                TextureAlongSpline();

            EditorGUILayout.Space();
            GUILayout.Label("Scatter Along (uses Scatter tab layer + palette)", EditorStyles.boldLabel);
            splineScatterSpacing = EditorGUILayout.Slider(new GUIContent("Spacing",
                "Distance between placements along the path, in meters."), splineScatterSpacing, 0.5f, 100f);
            splineScatterSide = GUILayout.Toolbar(splineScatterSide, new[]
            {
                new GUIContent("Center", "Place on the path itself."),
                new GUIContent("Left", "Place on the left side only."),
                new GUIContent("Right", "Place on the right side only."),
                new GUIContent("Both", "Place on both sides (guard rails, tree lines)."),
            });
            if (splineScatterSide != 0)
                splineScatterOffset = EditorGUILayout.Slider(new GUIContent("Side Offset",
                    "Lateral distance from the path center, in meters."), splineScatterOffset, 0f, 100f);
            splineScatterJitter = EditorGUILayout.Slider(new GUIContent("Jitter",
                "Random offset added to each placement for a natural look."), splineScatterJitter, 0f, 30f);
            bool scatterReady = layer != null && layer.palette != null && layer.palette.entries.Count > 0;
            if (!scatterReady)
                EditorGUILayout.HelpBox("Set up Layer + Palette in the Scatter tab first.", MessageType.Warning);
            using (new EditorGUI.DisabledScope(!scatterReady))
            {
                if (GUILayout.Button(new GUIContent("Scatter Along Spline",
                    "Place palette prefabs along the path with the settings above. Footprints and Min Distance apply. One undo step.")))
                    ScatterAlongSpline();
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
            float reach = splineWidth * 0.5f + splineFeather;
            float hardness = reach > 0f ? splineWidth * 0.5f / reach : 1f;
            int hit = 0;
            var meshes = new System.Collections.Generic.HashSet<MeshFilter>();
            SculptUndo.BeginStroke(SculptUndo.StrokeKind.Heights);
            try
            {
                foreach (Vector3 sample in samples)
                {
                    Terrain terrain = SplineOps.FindTerrainAt(sample);
                    if (terrain != null)
                    {
                        SplineOps.FlattenStamp(terrain, sample, splineWidth * 0.5f, splineFeather, sample.y,
                            splineBedNoise, splineBedNoiseScale, splineEdgeNoise, splineEdgeNoiseScale);
                        hit++;
                        continue;
                    }
                    MeshPaintableSurface meshSurface = RaycastMeshBelow(sample);
                    if (meshSurface == null) continue;
                    meshes.Add(meshSurface.Filter);
                    meshSurface.ApplyStamp(new SculptStampArgs
                    {
                        op = SculptOp.Flatten,
                        center = sample,
                        brushNormal = Vector3.up,
                        radius = reach + splineEdgeNoise,
                        strength = 1f,
                        hardness = hardness,
                        flattenPoint = sample,
                        flattenNormal = Vector3.up,
                        bedNoiseAmp = splineBedNoise,
                        bedNoiseScale = splineBedNoiseScale,
                        edgeNoiseAmp = splineEdgeNoise,
                        edgeNoiseScale = splineEdgeNoiseScale,
                    });
                    hit++;
                }
            }
            finally { SculptUndo.EndStroke(); }
            foreach (MeshFilter mf in meshes) MeshPaintableSurface.RefreshCollider(mf);
            lastStampWarning = hit == 0
                ? "Nothing under the spline (terrain or mesh with collider) — nothing flattened."
                : $"Flattened ground under {hit}/{samples.Count} spline samples.";
            SceneView.RepaintAll();
        }

        private MeshPaintableSurface RaycastMeshBelow(Vector3 position)
        {
            if (!Physics.Raycast(position + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 300f, surfaceMask, QueryTriggerInteraction.Ignore))
                return null;
            return MeshPaintableSurface.TryFrom(hit.collider);
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
                    if (terrain != null)
                    {
                        if (terrainLayer == null) continue;
                        new TerrainPaintableSurface(terrain).ApplyTexturePaint(sample, reach, 1f, hardness, terrainLayer);
                        hit++;
                        continue;
                    }
                    MeshPaintableSurface meshSurface = RaycastMeshBelow(sample);
                    if (meshSurface == null) continue;
                    meshSurface.ApplyVertexColor(sample, Vector3.up, reach, 1f, hardness, meshVertexColor);
                    hit++;
                }
            }
            finally { SculptUndo.EndStroke(); }
            lastStampWarning = hit == 0
                ? "Nothing paintable under the spline — terrains need a Terrain Layer, meshes get vertex color."
                : $"Painted along {hit}/{samples.Count} spline samples.";
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
                if (proceduralBrush == null) return;
                if (hit.collider is TerrainCollider)
                {
                    Terrain terrain = hit.collider.GetComponent<Terrain>();
                    if (terrain == null || terrain.terrainData == null) return;
                    if (!sculptStrokeStarted)
                    {
                        SculptUndo.BeginStroke(SculptUndo.StrokeKind.Heights);
                        sculptStrokeStarted = true;
                    }
                    ProceduralOps.Stamp(terrain, proceduralBrush, hit.point, radius, sculptStrength, sculptHardness, invert);
                }
                else
                {
                    MeshPaintableSurface meshSurface = MeshPaintableSurface.TryFrom(hit.collider);
                    if (meshSurface == null) return;
                    touchedMeshes.Add(meshSurface.Filter);
                    if (!sculptStrokeStarted)
                    {
                        SculptUndo.BeginStroke(SculptUndo.StrokeKind.Heights);
                        sculptStrokeStarted = true;
                    }
                    meshSurface.ApplyProcedural(proceduralBrush, hit.point, hit.normal, radius, sculptStrength, sculptHardness, invert);
                }
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
