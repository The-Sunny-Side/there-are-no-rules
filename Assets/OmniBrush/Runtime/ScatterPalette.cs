using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmniBrush
{
    [CreateAssetMenu(menuName = "OmniBrush/Scatter Palette", fileName = "ScatterPalette")]
    public class ScatterPalette : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Prefab painted by this entry.")]
            public GameObject prefab;
            [Min(0f), Tooltip("Relative pick probability. 1 and 1 = 50/50; 2 and 1 = twice as often. 0 = temporarily disabled.")]
            public float weight = 1f;
            [Tooltip("Random uniform scale range per instance (min X, max Y).")]
            public Vector2 uniformScale = new Vector2(0.8f, 1.2f);
            [Tooltip("Rotate each instance randomly around its up axis.")]
            public bool randomYaw = true;
            [Range(0f, 1f), Tooltip("0 = stays world-upright, 1 = fully tilts to match the surface slope.")]
            public float alignToNormal = 1f;
            [Tooltip("Shift along the instance's up axis in meters. Negative sinks it into the ground (rocks).")]
            public float verticalOffset;
            [Min(0f), Tooltip("Clear space around this prefab's pivot, scaled with the instance. Two placements keep footprintA + footprintB apart. 0 = only the brush's global Min Distance applies.")]
            public float footprintRadius = 0.5f;
        }

        public List<Entry> entries = new List<Entry>();

        private void OnValidate()
        {
            // Elements added with the inspector's "+" bypass field initializers
            // and arrive all-zero (weight 0 = never painted, scale 0 = invisible).
            // Heal them to usable defaults; intentional weight-0 entries keep
            // their non-zero scale and are left alone.
            foreach (Entry e in entries)
            {
                if (e.weight <= 0f && e.uniformScale == Vector2.zero && !e.randomYaw && e.alignToNormal == 0f)
                {
                    e.weight = 1f;
                    e.uniformScale = new Vector2(0.8f, 1.2f);
                    e.randomYaw = true;
                    e.alignToNormal = 1f;
                    e.footprintRadius = 0.5f;
                }
            }
        }

        /// <summary>Weighted random entry index, -1 if no valid entry. random01 in [0,1).</summary>
        public int PickWeightedIndex(float random01)
        {
            float total = 0f;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].prefab != null)
                    total += Mathf.Max(0f, entries[i].weight);
            if (total <= 0f) return -1;

            float t = random01 * total;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e.prefab == null) continue;
                float w = Mathf.Max(0f, e.weight);
                if (w <= 0f) continue;
                t -= w;
                if (t <= 0f) return i;
            }
            return -1;
        }
    }
}
