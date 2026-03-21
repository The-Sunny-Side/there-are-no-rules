#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Aggiunge automaticamente OutlineRendererFeature a tutti i renderer URP del progetto.
/// Esegui da Tools > Setup Outline Feature.
/// </summary>
public static class OutlineSetup
{
    [MenuItem("Tools/Setup Outline Feature")]
    public static void AddOutlineToAllRenderers()
    {
        string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");

        if (guids.Length == 0)
        {
            Debug.LogWarning("[OutlineSetup] Nessun UniversalRendererData trovato nel progetto.");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (rendererData == null) continue;

            bool alreadyPresent = false;
            foreach (var f in rendererData.rendererFeatures)
            {
                if (f is OutlineRendererFeature) { alreadyPresent = true; break; }
            }

            if (alreadyPresent)
            {
                Debug.Log($"[OutlineSetup] {rendererData.name}: OutlineRendererFeature già presente, skip.");
                continue;
            }

            var feature = ScriptableObject.CreateInstance<OutlineRendererFeature>();
            feature.name = "Outline";
            feature.settings.outlineColor    = Color.black;
            feature.settings.thickness       = 1.5f;
            feature.settings.depthThreshold  = 0.005f;
            feature.settings.normalThreshold = 0.4f;

            AssetDatabase.AddObjectToAsset(feature, path);
            rendererData.rendererFeatures.Add(feature);
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();

            Debug.Log($"[OutlineSetup] Outline aggiunto a: {rendererData.name}");
        }

        AssetDatabase.Refresh();
        Debug.Log("[OutlineSetup] Done!");
    }
}
#endif
