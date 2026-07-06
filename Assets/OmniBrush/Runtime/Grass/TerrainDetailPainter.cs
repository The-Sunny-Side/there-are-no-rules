using UnityEngine;

namespace OmniBrush
{
    /// <summary>
    /// Paints Unity terrain detail (grass) density. CPU dirty-rect ops with
    /// an undo record hook, mirroring the sculpt pipeline's shape.
    /// </summary>
    public static class TerrainDetailPainter
    {
        public delegate void DetailRecord(TerrainData data, int layer, int x, int y, int[,] before, int[,] after);

        /// <summary>Editor hook: receives per-stamp density diffs for undo.</summary>
        public static DetailRecord recordHook;

        /// <summary>
        /// Find or add a detail prototype for the given source (mesh prefab
        /// wins over texture). Returns the prototype index, -1 on failure.
        /// </summary>
        public static int EnsurePrototype(Terrain terrain, Texture2D texture, GameObject prefab)
        {
            TerrainData data = terrain.terrainData;
            DetailPrototype[] protos = data.detailPrototypes;
            for (int i = 0; i < protos.Length; i++)
            {
                if (prefab != null && protos[i].usePrototypeMesh && protos[i].prototype == prefab) return i;
                if (prefab == null && texture != null && !protos[i].usePrototypeMesh && protos[i].prototypeTexture == texture) return i;
            }
            if (prefab == null && texture == null) return -1;

            var proto = new DetailPrototype
            {
                minWidth = 0.8f, maxWidth = 1.6f,
                minHeight = 0.8f, maxHeight = 1.6f,
                noiseSpread = 0.4f,
                healthyColor = new Color(0.45f, 0.65f, 0.25f),
                dryColor = new Color(0.7f, 0.65f, 0.3f),
            };
            if (prefab != null)
            {
                proto.usePrototypeMesh = true;
                proto.prototype = prefab;
                proto.renderMode = DetailRenderMode.VertexLit;
                proto.useInstancing = true;
            }
            else
            {
                proto.usePrototypeMesh = false;
                proto.prototypeTexture = texture;
                proto.renderMode = DetailRenderMode.GrassBillboard;
            }
            System.Array.Resize(ref protos, protos.Length + 1);
            protos[protos.Length - 1] = proto;
            data.detailPrototypes = protos;
            return protos.Length - 1;
        }

        /// <summary>Paint (max toward target scaled by falloff) or erase (fade to 0) density.</summary>
        public static bool PaintDensity(Terrain terrain, int layer, Vector3 center, float radius,
            float strength, float hardness, int targetDensity, bool erase)
        {
            TerrainData data = terrain.terrainData;
            if (layer < 0 || layer >= data.detailPrototypes.Length) return false;

            int res = data.detailResolution;
            Vector3 local = center - terrain.transform.position;
            float cx = local.x / data.size.x * res;
            float cz = local.z / data.size.z * res;
            float rx = Mathf.Max(0.51f, radius / data.size.x * res);
            float rz = Mathf.Max(0.51f, radius / data.size.z * res);

            int x0 = Mathf.Clamp(Mathf.FloorToInt(cx - rx), 0, res - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(cx + rx), 0, res - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(cz - rz), 0, res - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(cz + rz), 0, res - 1);
            int w = x1 - x0 + 1, h = y1 - y0 + 1;
            if (w <= 0 || h <= 0) return false;

            int[,] map = data.GetDetailLayer(x0, y0, w, h, layer);
            var before = (int[,])map.Clone();
            bool changed = false;
            float clampedStrength = Mathf.Clamp01(strength);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float du = (x0 + x + 0.5f - cx) / rx;
                float dv = (y0 + y + 0.5f - cz) / rz;
                float weight = MeshPaintableSurface.Falloff(Mathf.Sqrt(du * du + dv * dv), hardness) * clampedStrength;
                if (weight <= 0f) continue;
                int v = map[y, x];
                int nv = erase
                    ? Mathf.Min(v, Mathf.RoundToInt(v * (1f - weight)))
                    : Mathf.Max(v, Mathf.RoundToInt(targetDensity * weight));
                if (nv != v) { map[y, x] = nv; changed = true; }
            }
            if (!changed) return false;

            data.SetDetailLayer(x0, y0, layer, map);
            recordHook?.Invoke(data, layer, x0, y0, before, (int[,])map.Clone());
            return true;
        }
    }
}
