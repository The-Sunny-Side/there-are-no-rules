using UnityEditor;
using UnityEngine;

namespace OmniBrush.Editor
{
    /// <summary>
    /// Terrain operations along splines. Flatten writes heights directly on
    /// the CPU (exact target, unlike the converging brush shader) with
    /// dirty-rect undo records through SculptUndo.
    /// </summary>
    public static class SplineOps
    {
        public static Terrain FindTerrainAt(Vector3 position)
        {
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                if (terrain.terrainData == null) continue;
                Vector3 local = position - terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (local.x >= 0f && local.x <= size.x && local.z >= 0f && local.z <= size.z)
                    return terrain;
            }
            return null;
        }

        /// <summary>Blend heights toward targetY in a disc: flat core + smoothstep feather.</summary>
        public static void FlattenStamp(Terrain terrain, Vector3 center, float flatRadius, float feather, float targetY)
        {
            TerrainData data = terrain.terrainData;
            int res = data.heightmapResolution;
            Vector3 origin = terrain.transform.position;
            float pxX = data.size.x / (res - 1);
            float pxZ = data.size.z / (res - 1);
            float reach = flatRadius + Mathf.Max(0f, feather);

            int cx = Mathf.RoundToInt((center.x - origin.x) / pxX);
            int cy = Mathf.RoundToInt((center.z - origin.z) / pxZ);
            int rx = Mathf.CeilToInt(reach / pxX);
            int ry = Mathf.CeilToInt(reach / pxZ);
            int x0 = Mathf.Clamp(cx - rx, 0, res - 1);
            int x1 = Mathf.Clamp(cx + rx, 0, res - 1);
            int y0 = Mathf.Clamp(cy - ry, 0, res - 1);
            int y1 = Mathf.Clamp(cy + ry, 0, res - 1);
            int w = x1 - x0 + 1, h = y1 - y0 + 1;
            if (w <= 0 || h <= 0) return;

            float[,] heights = data.GetHeights(x0, y0, w, h);
            var before = (float[,])heights.Clone();
            float target01 = Mathf.Clamp01((targetY - origin.y) / data.size.y);
            bool changed = false;
            for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
            {
                float wx = origin.x + (x0 + px) * pxX;
                float wz = origin.z + (y0 + py) * pxZ;
                float d = Mathf.Sqrt((wx - center.x) * (wx - center.x) + (wz - center.z) * (wz - center.z));
                if (d > reach) continue;
                float weight;
                if (d <= flatRadius) weight = 1f;
                else if (feather <= 0f) continue;
                else
                {
                    float t = (d - flatRadius) / feather;
                    weight = 1f - t * t * (3f - 2f * t);
                }
                float blended = Mathf.Lerp(heights[py, px], target01, weight);
                if (!Mathf.Approximately(blended, heights[py, px]))
                {
                    heights[py, px] = blended;
                    changed = true;
                }
            }
            if (!changed) return;

            data.SetHeights(x0, y0, heights);
            SculptUndo.RecordHeights(data, x0, y0, before, (float[,])heights.Clone());
        }
    }
}
