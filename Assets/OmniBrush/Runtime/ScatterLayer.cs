using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OmniBrush
{
    [Serializable]
    public struct ScatterInstance
    {
        public int entryIndex;
        public Vector3 position; // world space
        public Quaternion rotation;
        public Vector3 scale;
    }

    /// <summary>
    /// Owns scatter data (world-space instances) and renders it with GPU
    /// instancing in edit and play mode. No GameObjects exist until baked
    /// (see ScatterLayerEditor). The layer's own transform is ignored.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ScatterLayer : MonoBehaviour
    {
        public ScatterPalette palette;
        public ShadowCastingMode castShadows = ShadowCastingMode.On;
        public bool receiveShadows = true;

        [SerializeField, HideInInspector] private List<ScatterInstance> instances = new List<ScatterInstance>();

        public int Count => instances.Count;
        public IReadOnlyList<ScatterInstance> Instances => instances;

        // ------------------------------------------------------------- data API

        public void AddInstance(in ScatterInstance instance)
        {
            instances.Add(instance);
            if (hash != null && !hashDirty)
                HashAdd(instance.position, instances.Count - 1);
            batchesDirty = true;
        }

        public int RemoveInRadius(Vector3 center, float radius)
        {
            float sqr = radius * radius;
            int before = instances.Count;
            instances.RemoveAll(i => (i.position - center).sqrMagnitude <= sqr);
            int removed = before - instances.Count;
            if (removed > 0) MarkDirty();
            return removed;
        }

        public void ClearAll()
        {
            instances.Clear();
            MarkDirty();
        }

        /// <summary>Invalidate caches after external data changes (undo/redo).</summary>
        public void MarkDirty()
        {
            hashDirty = true;
            batchesDirty = true;
        }

        /// <summary>Invalidate cached prefab render data after palette edits.</summary>
        public void MarkPaletteDirty()
        {
            partsPerEntry = null;
            batchesDirty = true;
        }

        // ------------------------------------------------------- neighbor query

        private const float CellSize = 4f;
        [NonSerialized] private Dictionary<long, List<int>> hash;
        [NonSerialized] private bool hashDirty = true;

        private static long CellKey(int x, int z) => ((long)x << 32) | ((long)z & 0xffffffffL);

        private void HashAdd(Vector3 pos, int index)
        {
            long key = CellKey(Mathf.FloorToInt(pos.x / CellSize), Mathf.FloorToInt(pos.z / CellSize));
            if (!hash.TryGetValue(key, out List<int> list))
                hash[key] = list = new List<int>(8);
            list.Add(index);
        }

        private void EnsureHash()
        {
            if (hash != null && !hashDirty) return;
            hash = new Dictionary<long, List<int>>(Mathf.Max(16, instances.Count / 4));
            for (int i = 0; i < instances.Count; i++)
                HashAdd(instances[i].position, i);
            hashDirty = false;
        }

        public bool HasInstanceWithin(Vector3 pos, float minDistance)
        {
            if (instances.Count == 0) return false;
            EnsureHash();
            float sqr = minDistance * minDistance;
            int x0 = Mathf.FloorToInt((pos.x - minDistance) / CellSize);
            int x1 = Mathf.FloorToInt((pos.x + minDistance) / CellSize);
            int z0 = Mathf.FloorToInt((pos.z - minDistance) / CellSize);
            int z1 = Mathf.FloorToInt((pos.z + minDistance) / CellSize);
            for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
            {
                if (!hash.TryGetValue(CellKey(x, z), out List<int> list)) continue;
                for (int i = 0; i < list.Count; i++)
                    if ((instances[list[i]].position - pos).sqrMagnitude < sqr)
                        return true;
            }
            return false;
        }

        // ------------------------------------------------------------ rendering

        private struct RenderPart
        {
            public Mesh mesh;
            public Material[] materials;
            public Matrix4x4 localToRoot;
            public Bounds meshBounds;
        }

        private class Batch
        {
            public Mesh mesh;
            public int submesh;
            public Material material;
            public Matrix4x4[] matrices;
            public int count;
            public Bounds bounds;
        }

        private const int MaxInstancesPerBatch = 1023;

        [NonSerialized] private List<RenderPart>[] partsPerEntry;
        [NonSerialized] private ScatterPalette cachedPalette;
        [NonSerialized] private List<Batch> batches;
        [NonSerialized] private bool batchesDirty = true;
        private static readonly Plane[] FrustumCache = new Plane[6];
        private static readonly HashSet<Material> WarnedMaterials = new HashSet<Material>();

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraSrp;
            Camera.onPreCull += OnPreCullBuiltin;
            MarkDirty();
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraSrp;
            Camera.onPreCull -= OnPreCullBuiltin;
            batches = null;
            partsPerEntry = null;
        }

        private void OnBeginCameraSrp(ScriptableRenderContext context, Camera cam)
        {
            if (GraphicsSettings.currentRenderPipeline == null) return;
            Render(cam);
        }

        private void OnPreCullBuiltin(Camera cam)
        {
            if (GraphicsSettings.currentRenderPipeline != null) return;
            Render(cam);
        }

        private void Render(Camera cam)
        {
            if (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView) return;
            if (palette == null || instances.Count == 0) return;
            if (batchesDirty || batches == null) RebuildBatches();
            if (batches.Count == 0) return;

            GeometryUtility.CalculateFrustumPlanes(cam, FrustumCache);
            for (int i = 0; i < batches.Count; i++)
            {
                Batch b = batches[i];
                if (!GeometryUtility.TestPlanesAABB(FrustumCache, b.bounds)) continue;
                Graphics.DrawMeshInstanced(b.mesh, b.submesh, b.material, b.matrices, b.count,
                    null, castShadows, receiveShadows, gameObject.layer, cam);
            }
        }

        private void EnsureRenderParts()
        {
            if (partsPerEntry != null && cachedPalette == palette &&
                partsPerEntry.Length == palette.entries.Count) return;

            cachedPalette = palette;
            partsPerEntry = new List<RenderPart>[palette.entries.Count];
            for (int e = 0; e < palette.entries.Count; e++)
            {
                List<RenderPart> parts = partsPerEntry[e] = new List<RenderPart>();
                GameObject prefab = palette.entries[e].prefab;
                if (prefab == null) continue;

                // LOD0 renderers only, when the prefab root has a LODGroup.
                HashSet<Renderer> lod0 = null;
                LODGroup lodGroup = prefab.GetComponent<LODGroup>();
                if (lodGroup != null)
                {
                    LOD[] lods = lodGroup.GetLODs();
                    if (lods.Length > 0) lod0 = new HashSet<Renderer>(lods[0].renderers);
                }

                Matrix4x4 rootWorldToLocal = prefab.transform.worldToLocalMatrix;
                foreach (MeshRenderer mr in prefab.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!mr.enabled) continue;
                    if (lod0 != null && !lod0.Contains(mr)) continue;
                    MeshFilter mf = mr.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    parts.Add(new RenderPart
                    {
                        mesh = mf.sharedMesh,
                        materials = mr.sharedMaterials,
                        localToRoot = rootWorldToLocal * mr.transform.localToWorldMatrix,
                        meshBounds = mf.sharedMesh.bounds,
                    });
                }
            }
        }

        private void RebuildBatches()
        {
            batchesDirty = false;
            if (batches == null) batches = new List<Batch>();
            batches.Clear();
            if (palette == null) return;
            EnsureRenderParts();

            var perEntry = new List<Matrix4x4>[partsPerEntry.Length];
            for (int i = 0; i < instances.Count; i++)
            {
                ScatterInstance inst = instances[i];
                if (inst.entryIndex < 0 || inst.entryIndex >= partsPerEntry.Length) continue;
                List<Matrix4x4> list = perEntry[inst.entryIndex];
                if (list == null) list = perEntry[inst.entryIndex] = new List<Matrix4x4>();
                list.Add(Matrix4x4.TRS(inst.position, inst.rotation, inst.scale));
            }

            for (int e = 0; e < partsPerEntry.Length; e++)
            {
                List<Matrix4x4> matrices = perEntry[e];
                if (matrices == null) continue;
                foreach (RenderPart part in partsPerEntry[e])
                {
                    for (int start = 0; start < matrices.Count; start += MaxInstancesPerBatch)
                    {
                        int count = Mathf.Min(MaxInstancesPerBatch, matrices.Count - start);
                        var arr = new Matrix4x4[count];
                        Bounds bounds = default;
                        for (int k = 0; k < count; k++)
                        {
                            Matrix4x4 m = matrices[start + k] * part.localToRoot;
                            arr[k] = m;
                            Bounds wb = TransformBounds(part.meshBounds, m);
                            if (k == 0) bounds = wb;
                            else { bounds.Encapsulate(wb.min); bounds.Encapsulate(wb.max); }
                        }
                        for (int sm = 0; sm < part.mesh.subMeshCount; sm++)
                        {
                            Material material = part.materials.Length > 0
                                ? part.materials[Mathf.Min(sm, part.materials.Length - 1)]
                                : null;
                            if (material == null) continue;
                            if (!material.enableInstancing)
                            {
                                if (WarnedMaterials.Add(material))
                                    Debug.LogWarning(
                                        $"[OmniBrush] Material '{material.name}' has GPU Instancing disabled — its instances are skipped. Use the fix button on the ScatterLayer inspector.",
                                        material);
                                continue;
                            }
                            batches.Add(new Batch
                            {
                                mesh = part.mesh, submesh = sm, material = material,
                                matrices = arr, count = count, bounds = bounds,
                            });
                        }
                    }
                }
            }
        }

        private static Bounds TransformBounds(Bounds local, Matrix4x4 m)
        {
            Vector3 center = m.MultiplyPoint3x4(local.center);
            Vector3 ext = local.extents;
            Vector3 ax = m.MultiplyVector(new Vector3(ext.x, 0f, 0f));
            Vector3 ay = m.MultiplyVector(new Vector3(0f, ext.y, 0f));
            Vector3 az = m.MultiplyVector(new Vector3(0f, 0f, ext.z));
            var worldExt = new Vector3(
                Mathf.Abs(ax.x) + Mathf.Abs(ay.x) + Mathf.Abs(az.x),
                Mathf.Abs(ax.y) + Mathf.Abs(ay.y) + Mathf.Abs(az.y),
                Mathf.Abs(ax.z) + Mathf.Abs(ay.z) + Mathf.Abs(az.z));
            return new Bounds(center, worldExt * 2f);
        }
    }
}
