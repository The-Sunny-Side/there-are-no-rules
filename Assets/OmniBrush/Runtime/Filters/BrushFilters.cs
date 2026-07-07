using UnityEngine;

namespace OmniBrush
{
    /// <summary>
    /// Procedural placement filters shared by brushes: noise mask, splat
    /// weight under the candidate, and surface curvature via neighbor probes.
    /// </summary>
    public static class BrushFilters
    {
        /// <summary>Perlin gate: true where noise(worldXZ/scale) >= threshold. Deterministic per position.</summary>
        public static bool PassesNoise(Vector3 worldPos, float scale, float threshold)
        {
            if (scale < 0.01f) scale = 0.01f;
            // large offset keeps sampling away from PerlinNoise's mirrored origin
            float n = Mathf.PerlinNoise(worldPos.x / scale + 10000f, worldPos.z / scale + 10000f);
            return n >= threshold;
        }

        /// <summary>
        /// Weight of a TerrainLayer in the splat map under the hit.
        /// Returns -1 when the hit is not a terrain (caller should pass),
        /// 0 when the layer isn't on that terrain.
        /// </summary>
        public static float SampleLayerWeight(in RaycastHit hit, TerrainLayer layer)
        {
            var terrainCollider = hit.collider as TerrainCollider;
            if (terrainCollider == null || layer == null) return -1f;
            Terrain terrain = terrainCollider.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null) return -1f;

            TerrainData data = terrain.terrainData;
            TerrainLayer[] layers = data.terrainLayers;
            int index = -1;
            for (int i = 0; i < layers.Length; i++)
                if (layers[i] == layer) { index = i; break; }
            if (index < 0) return 0f;

            Vector3 local = hit.point - terrain.transform.position;
            int x = Mathf.Clamp(Mathf.RoundToInt(local.x / data.size.x * (data.alphamapWidth - 1)), 0, data.alphamapWidth - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(local.z / data.size.z * (data.alphamapHeight - 1)), 0, data.alphamapHeight - 1);
            float[,,] alpha = data.GetAlphamaps(x, y, 1, 1);
            return alpha[0, 0, index];
        }

        /// <summary>
        /// Average height of 4 neighbors relative to the point, along the
        /// surface normal. Positive = neighbors sit higher = the point is in
        /// a hollow (concave); negative = on a bump (convex).
        /// </summary>
        public static float SampleRelativeHeight(Vector3 point, Vector3 normal, float sampleDistance, LayerMask mask)
        {
            Vector3 n = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
            Vector3 t = Vector3.Cross(n, Vector3.up);
            if (t.sqrMagnitude < 1e-6f) t = Vector3.right;
            t.Normalize();
            Vector3 b = Vector3.Cross(n, t);

            float sum = 0f;
            int hits = 0;
            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = i == 0 ? t : i == 1 ? -t : i == 2 ? b : -b;
                Vector3 origin = point + dir * sampleDistance + n * sampleDistance;
                if (Physics.Raycast(origin, -n, out RaycastHit hit, sampleDistance * 4f, mask, QueryTriggerInteraction.Ignore))
                {
                    sum += Vector3.Dot(hit.point - point, n);
                    hits++;
                }
            }
            return hits > 0 ? sum / hits : 0f;
        }
    }
}
