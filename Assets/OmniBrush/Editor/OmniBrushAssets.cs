using UnityEditor;
using UnityEngine;

namespace OmniBrush.Editor
{
    /// <summary>
    /// Canonical home for user-created OmniBrush assets. Lives OUTSIDE
    /// Assets/OmniBrush so tool updates never touch user content.
    /// </summary>
    public static class OmniBrushAssets
    {
        private const string Root = "Assets/OmniBrushData";

        public static string EnsureFolder(string subfolder)
        {
            if (!AssetDatabase.IsValidFolder(Root))
                AssetDatabase.CreateFolder("Assets", "OmniBrushData");
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
