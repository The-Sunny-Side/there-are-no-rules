using UnityEditor;
using UnityEngine;

namespace OmniBrush.Editor
{
    /// <summary>
    /// Canonical home for user-created OmniBrush assets:
    /// Assets/OmniBrush/Data/&lt;Palettes|Brushes&gt;. NOTE for packaging:
    /// exclude Data/ when exporting the Asset Store package — package imports
    /// only add/overwrite files, so user content in Data/ survives updates.
    /// </summary>
    public static class OmniBrushAssets
    {
        private const string Root = "Assets/OmniBrush/Data";

        public static string EnsureFolder(string subfolder)
        {
            if (!AssetDatabase.IsValidFolder(Root))
                AssetDatabase.CreateFolder("Assets/OmniBrush", "Data");
            string path = Root + "/" + subfolder;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(Root, subfolder);
            return path;
        }

        /// <summary>Create an asset with a unique name in the standard folder and ping it.</summary>
        public static T CreateAsset<T>(string subfolder, string baseName) where T : ScriptableObject
        {
            string folder = EnsureFolder(subfolder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}.asset");
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"[OmniBrush] Created {path}", asset);
            return asset;
        }
    }
}
