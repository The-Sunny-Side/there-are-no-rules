using UnityEngine;

namespace OmniBrush
{
    public enum SculptOp { Raise, Lower, Smooth, Flatten, Stamp }

    public struct SculptStampArgs
    {
        public SculptOp op;
        public Vector3 center;       // world
        public float radius;         // world
        public float strength;       // 0..1
        public float hardness;       // 0..1 brush falloff hardness
        public float rotation;       // degrees, brush rotation
        public float flattenHeight;  // world Y, Flatten only
        public Texture2D stampTexture; // optional heightmap shape; falloff brush if null
        public float stampHeight;    // world meters above terrain base, Stamp only
        public bool stampAdditive;   // false = max blend (idempotent), true = add
    }

    /// <summary>A surface whose shape can be modified by sculpt brushes.</summary>
    public interface IPaintableSurface
    {
        Object Target { get; }
        bool ApplyStamp(SculptStampArgs args);
    }
}
