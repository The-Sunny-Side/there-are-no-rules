using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OmniBrush.Editor
{
    /// <summary>
    /// GPU Instancing checks/fixes for palette prefab materials. Instances of
    /// materials without instancing are skipped by ScatterLayer's renderer —
    /// i.e. invisible — so the UI surfaces this loudly.
    /// </summary>
    internal static class ScatterMaterialUtility
    {
        public static int CountMissingInstancing(ScatterPalette palette)
        {
            if (palette == null) return 0;
            var seen = new HashSet<Material>();
            int missing = 0;
            foreach (ScatterPalette.Entry entry in palette.entries)
                missing += CountMissingInstancing(entry.prefab, seen);
            return missing;
        }

        public static int CountMissingInstancing(GameObject prefab, HashSet<Material> seen = null)
        {
            if (prefab == null) return 0;
            seen = seen ?? new HashSet<Material>();
            int missing = 0;
            foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>())
            foreach (Material material in renderer.sharedMaterials)
                if (material != null && seen.Add(material) && !material.enableInstancing)
                    missing++;
            return missing;
        }

        public static int EnableInstancing(ScatterPalette palette)
        {
            if (palette == null) return 0;
            var seen = new HashSet<Material>();
            int fixedCount = 0;
            foreach (ScatterPalette.Entry entry in palette.entries)
                fixedCount += EnableInstancing(entry.prefab, seen);
            return fixedCount;
        }

        public static int EnableInstancing(GameObject prefab, HashSet<Material> seen = null)
        {
            if (prefab == null) return 0;
            seen = seen ?? new HashSet<Material>();
            int fixedCount = 0;
            foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>())
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || !seen.Add(material) || material.enableInstancing) continue;
                Undo.RecordObject(material, "Enable GPU Instancing");
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);
                fixedCount++;
            }
            return fixedCount;
        }
    }
}
