using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmniBrush
{
    /// <summary>
    /// Node-brush core as a layer stack: each layer produces a value from the
    /// world XZ position and blends over the previous result. The sculpt
    /// "Proc" op paints the stack's output (meters) through the brush falloff.
    /// Deterministic per position — repainting an area regenerates the same
    /// pattern seamlessly. A visual graph UI can sit on this model later.
    /// </summary>
    [CreateAssetMenu(menuName = "OmniBrush/Procedural Brush", fileName = "ProceduralBrush")]
    public class ProceduralBrush : ScriptableObject
    {
        public enum LayerType { Constant, Noise }
        public enum BlendMode { Add, Subtract, Multiply, Min, Max }

        [Serializable]
        public class Layer
        {
            public bool enabled = true;
            public LayerType type = LayerType.Noise;
            public BlendMode blend = BlendMode.Add;
            [Tooltip("Output height in meters (Constant) or noise amplitude (Noise).")]
            public float amplitude = 5f;
            [Min(0.1f)] public float noiseScale = 40f;
            [Range(1, 6)] public int octaves = 3;
            [Tooltip("abs() the noise for sharp ridges.")]
            public bool ridged;
            public Vector2 offset;
        }

        public List<Layer> layers = new List<Layer>
        {
            new Layer(),
        };

        /// <summary>Stack output in meters at a world XZ position.</summary>
        public float Evaluate(float worldX, float worldZ)
        {
            float value = 0f;
            for (int i = 0; i < layers.Count; i++)
            {
                Layer layer = layers[i];
                if (!layer.enabled) continue;
                float v;
                if (layer.type == LayerType.Constant)
                {
                    v = layer.amplitude;
                }
                else
                {
                    // fbm perlin, away from the mirrored origin
                    float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
                    for (int o = 0; o < layer.octaves; o++)
                    {
                        float n = Mathf.PerlinNoise(
                            (worldX + layer.offset.x) / layer.noiseScale * freq + 10000f,
                            (worldZ + layer.offset.y) / layer.noiseScale * freq + 10000f);
                        if (layer.ridged) n = 1f - Mathf.Abs(n * 2f - 1f);
                        sum += n * amp;
                        norm += amp;
                        amp *= 0.5f;
                        freq *= 2f;
                    }
                    v = sum / Mathf.Max(norm, 1e-5f) * layer.amplitude;
                }

                switch (layer.blend)
                {
                    case BlendMode.Add: value += v; break;
                    case BlendMode.Subtract: value -= v; break;
                    case BlendMode.Multiply: value *= v; break;
                    case BlendMode.Min: value = Mathf.Min(value, v); break;
                    case BlendMode.Max: value = Mathf.Max(value, v); break;
                }
            }
            return value;
        }
    }
}
