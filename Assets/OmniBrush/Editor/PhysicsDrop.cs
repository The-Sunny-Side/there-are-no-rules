using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OmniBrush.Editor
{
    /// <summary>
    /// Settles scattered instances with an in-editor physics simulation:
    /// temporary rigidbodies (convex mesh collider from the prefab, box
    /// fallback) drop, roll and pile against scene colliders and each other,
    /// then the settled transforms are written back as instance data in a
    /// single undo step. No play mode involved.
    /// </summary>
    public static class PhysicsDrop
    {
        private const float TimeStep = 0.02f;

        public static int Settle(ScatterLayer layer, float dropHeight, float maxSeconds)
        {
            if (layer == null || layer.palette == null || layer.Count == 0) return 0;
            ScatterPalette palette = layer.palette;

            Undo.RegisterCompleteObjectUndo(layer, "OmniBrush Physics Drop");

            var root = new GameObject("__OmniBrushPhysicsDrop") { hideFlags = HideFlags.HideAndDontSave };
            var bodies = new List<Rigidbody>(layer.Count);
            var indices = new List<int>(layer.Count);
            SimulationMode previousMode = Physics.simulationMode;
            try
            {
                for (int i = 0; i < layer.Count; i++)
                {
                    ScatterInstance instance = layer.Instances[i];
                    if (instance.entryIndex < 0 || instance.entryIndex >= palette.entries.Count) continue;
                    GameObject prefab = palette.entries[instance.entryIndex].prefab;
                    if (prefab == null) continue;

                    var go = new GameObject("drop");
                    go.transform.SetParent(root.transform, false);
                    go.transform.SetPositionAndRotation(instance.position + Vector3.up * dropHeight, instance.rotation);
                    go.transform.localScale = instance.scale;
                    AttachCollider(go, prefab);
                    bodies.Add(go.AddComponent<Rigidbody>());
                    indices.Add(i);
                }
                if (bodies.Count == 0) return 0;

                Physics.SyncTransforms();
                Physics.simulationMode = SimulationMode.Script;
                int steps = Mathf.CeilToInt(Mathf.Max(0.5f, maxSeconds) / TimeStep);
                for (int s = 0; s < steps; s++)
                {
                    Physics.Simulate(TimeStep);
                    if (s % 15 != 14) continue;
                    bool allSleeping = true;
                    for (int b = 0; b < bodies.Count; b++)
                        if (!bodies[b].IsSleeping()) { allSleeping = false; break; }
                    if (allSleeping) break;
                }

                for (int b = 0; b < bodies.Count; b++)
                    layer.UpdateInstance(indices[b], bodies[b].position, bodies[b].rotation);
                EditorUtility.SetDirty(layer);
                SceneView.RepaintAll();
                return bodies.Count;
            }
            finally
            {
                Physics.simulationMode = previousMode;
                Object.DestroyImmediate(root);
            }
        }

        private static void AttachCollider(GameObject go, GameObject prefab)
        {
            MeshFilter meshFilter = prefab.GetComponentInChildren<MeshFilter>();
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (mesh != null && mesh.vertexCount >= 4)
            {
                var child = new GameObject("col");
                child.transform.SetParent(go.transform, false);
                Matrix4x4 localToRoot = prefab.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                child.transform.localPosition = localToRoot.GetColumn(3);
                child.transform.localRotation = localToRoot.rotation;
                child.transform.localScale = localToRoot.lossyScale;
                var meshCollider = child.AddComponent<MeshCollider>();
                meshCollider.convex = true; // PhysX decimates hulls above 255 polys
                meshCollider.sharedMesh = mesh;
                return;
            }
            var box = go.AddComponent<BoxCollider>();
            if (mesh != null)
            {
                box.center = mesh.bounds.center;
                box.size = mesh.bounds.size;
            }
        }
    }
}
