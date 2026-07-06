using UnityEngine;

namespace OmniBrush
{
    /// <summary>Procedural radial falloff brush textures, cached per hardness step.</summary>
    public static class SculptBrushTexture
    {
        private const int Resolution = 128;
        private static readonly Texture2D[] Cache = new Texture2D[11]; // hardness 0..1 in 0.1 steps

        public static Texture2D Get(float hardness)
        {
            int slot = Mathf.Clamp(Mathf.RoundToInt(hardness * 10f), 0, 10);
            if (Cache[slot] != null) return Cache[slot];

            float h = slot / 10f;
            var tex = new Texture2D(Resolution, Resolution, TextureFormat.RFloat, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            float half = (Resolution - 1) * 0.5f;
            for (int y = 0; y < Resolution; y++)
            for (int x = 0; x < Resolution; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                float v;
                if (d >= 1f) v = 0f;
                else
                {
                    float t = h >= 0.999f ? 0f : Mathf.Clamp01((d - h) / (1f - h));
                    v = 1f - t * t * (3f - 2f * t); // smoothstep falloff from hardness edge
                }
                tex.SetPixel(x, y, new Color(v, v, v, v));
            }
            tex.Apply(false, true);
            Cache[slot] = tex;
            return tex;
        }
    }
}
