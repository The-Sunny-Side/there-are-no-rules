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
            public GameObject prefab;
            [Min(0f)] public float weight = 1f;
            public Vector2 uniformScale = new Vector2(0.8f, 1.2f);
            public bool randomYaw = true;
            [Range(0f, 1f)] public float alignToNormal = 1f;
            public float verticalOffset;
        }

        public List<Entry> entries = new List<Entry>();

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
