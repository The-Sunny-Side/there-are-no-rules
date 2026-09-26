using UnityEngine;

namespace OmniBrush
{
    public enum SculptOp { Raise, Lower, Smooth, Flatten, Stamp }

    public struct SculptStampArgs
    {
        public SculptOp op;
        public Vector3 center;       // world
        public Vector3 brushNormal;  // current hit normal (brush axis on meshes)
        public float radius;         // world
        public float strength;       // 0..1
        public float hardness;       // 0..1 brush falloff hardness
        public float rotation;       // degrees, brush rotation
        public float flattenHeight;  // world Y, Flatten on terrain
        public Vector3 flattenPoint; // stroke-start hit point (flatten plane / stamp frame on meshes)
        public Vector3 flattenNormal;// stroke-start hit normal
        public Texture2D stampTexture; // optional heightmap shape; falloff brush if null
        public float stampHeight;    // world meters above terrain base, Stamp only
        public bool stampAdditive;   // false = max blend (idempotent), true = add
        public float bedNoiseAmp;    // Flatten: Perlin wobble of the target plane, meters
        public float bedNoiseScale;  // meters
        public float edgeNoiseAmp;   // Flatten: Perlin wobble of the brush border, meters
        public float edgeNoiseScale; // meters
    }

    /// <summary>A surface whose shape can be modified by sculpt brushes.</summary>
    public interface IPaintableSurface
    {
        Object Target { get; }
        bool ApplyStamp(SculptStampArgs args);
    }
}
