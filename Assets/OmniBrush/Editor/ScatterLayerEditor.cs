using UnityEditor;
using UnityEngine;

namespace OmniBrush.Editor
{
    [CustomEditor(typeof(ScatterLayer))]
    public class ScatterLayerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var layer = (ScatterLayer)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Instances", layer.Count.ToString());

            if (GUILayout.Button("Open Brush")) OmniBrushWindow.Open(layer);
            if (GUILayout.Button("Refresh Render Cache"))
            {
                layer.MarkPaletteDirty();
                layer.MarkDirty();
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("Enable GPU Instancing On Palette Materials")) EnableInstancing(layer);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(layer.Count == 0))
            {
                if (GUILayout.Button($"Bake {layer.Count} Instances To GameObjects")) Bake(layer);
            }
            EditorGUILayout.HelpBox("Instances are pure data rendered with GPU instancing — no colliders until baked.", MessageType.None);
        }

        private static void EnableInstancing(ScatterLayer layer)
        {
            int fixedCount = ScatterMaterialUtility.EnableInstancing(layer.palette);
            layer.MarkPaletteDirty();
            layer.MarkDirty();
            SceneView.RepaintAll();
            Debug.Log($"[OmniBrush] Enabled GPU Instancing on {fixedCount} material(s).");
        }

        private static void Bake(ScatterLayer layer)
        {
            if (layer.palette == null) return;
            if (layer.Count > 5000 && !EditorUtility.DisplayDialog("OmniBrush",
                    $"Bake {layer.Count} instances to GameObjects? This can take a while and bloats the scene.",
                    "Bake", "Cancel"))
                return;

            Undo.SetCurrentGroupName("OmniBrush Bake");
            int group = Undo.GetCurrentGroup();

            var container = new GameObject($"{layer.name}_Baked");
            container.transform.SetParent(layer.transform, false);
            Undo.RegisterCreatedObjectUndo(container, "OmniBrush Bake");

            int skipped = 0;
            foreach (ScatterInstance instance in layer.Instances)
            {
                GameObject prefab = instance.entryIndex >= 0 && instance.entryIndex < layer.palette.entries.Count
                    ? layer.palette.entries[instance.entryIndex].prefab
                    : null;
                if (prefab == null) { skipped++; continue; }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container.transform);
                go.transform.SetPositionAndRotation(instance.position, instance.rotation);
                go.transform.localScale = Vector3.Scale(prefab.transform.localScale, instance.scale);
            }

            Undo.RegisterCompleteObjectUndo(layer, "OmniBrush Bake");
            layer.ClearAll();
            EditorUtility.SetDirty(layer);
            Undo.CollapseUndoOperations(group);
            if (skipped > 0)
                Debug.LogWarning($"[OmniBrush] Skipped {skipped} instance(s) with missing palette entries.");
        }
    }
}
