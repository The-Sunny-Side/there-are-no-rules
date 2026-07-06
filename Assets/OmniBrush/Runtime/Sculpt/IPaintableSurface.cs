using UnityEngine;

namespace OmniBrush
{
    public enum SculptOp { Raise, Lower, Smooth, Flatten }

    public struct SculptStampArgs
    {
        public SculptOp op;
        public Vector3 center;      // world
        public float radius;        // world
        public float strength;      // 0..1
        public float hardness;      // 0..1 brush falloff hardness
        public float flattenHeight; // world Y, Flatten only
    }

    /// <summary>A surface whose shape can be modified by sculpt brushes.</summary>
    public interface IPaintableSurface
    {
        Object Target { get; }
        bool ApplyStamp(SculptStampArgs args);
    }
}
