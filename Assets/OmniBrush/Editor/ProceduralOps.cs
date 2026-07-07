using UnityEditor;
using UnityEngine;

namespace OmniBrush.Editor
{
    /// <summary>
    /// CPU stamp for the procedural (node-stack) sculpt op: adds the brush
    /// stack's height output through the falloff, with dirty-rect undo.
    /// </summary>
    public static class ProceduralOps
    {
        public static void Stamp(Terrain terrain, ProceduralBrush brush, Vector3 center,
            float radius, float strength, float hardness, bool invert)
        {
            TerrainData data = terrain.terrainData;
            int res = data.heightmapResolution;
            Vector3 origin = terrain.transform.position;
            float pxX = data.size.x / (res - 1);
            float pxZ = data.size.z / (res - 1);

            int cx = Mathf.RoundToInt((center.x - origin.x) / pxX);
            int cy = Mathf.RoundToInt((center.z - origin.z) / pxZ);
            int rx = Mathf.CeilToInt(radius / pxX);
            int ry = Mathf.CeilToInt(radius / pxZ);
            int x0 = Mathf.Clamp(cx - rx, 0, res - 1);
            int x1 = Mathf.Clamp(cx + rx, 0, res - 1);
            int y0 = Mathf.Clamp(cy - ry, 0, res - 1);
            int y1 = Mathf.Clamp(cy + ry, 0, res - 1);
            int w = x1 - x0 + 1, h = y1 - y0 + 1;
            if (w <= 0 || h <= 0) return;

            float[,] heights = data.GetHeights(x0, y0, w, h);
            var before = (float[,])heights.Clone();
            float sign = invert ? -1f : 1f;
            float scaleY = data.size.y;
            bool changed = false;
            for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
            {
                float wx = origin.x + (x0 + px) * pxX;
                float wz = origin.z + (y0 + py) * pxZ;
                float d01 = Mathf.Sqrt((wx - center.x) * (wx - center.x) + (wz - center.z) * (wz - center.z)) / radius;
                float weight = MeshPaintableSurface.Falloff(d01, hardness) * Mathf.Clamp01(strength);
                if (weight <= 0f) continue;

                float deltaMeters = brush.Evaluate(wx, wz) * weight * sign;
                float value = Mathf.Clamp01(heights[py, px] + deltaMeters / scaleY);
                if (!Mathf.Approximately(value, heights[py, px]))
                {
                    heights[py, px] = value;
                    changed = true;
                }
            }
            if (!changed) return;

            data.SetHeights(x0, y0, heights);
            SculptUndo.RecordHeights(data, x0, y0, before, (float[,])heights.Clone());
        }
    }
}
