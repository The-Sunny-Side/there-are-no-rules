using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class AssetCompressionChecker : EditorWindow
{
    private Vector2 scrollPos;
    private List<string> uncompressedTextures = new List<string>();
    private List<string> uncompressedAudio = new List<string>();
    
    [MenuItem("Tools/Check Asset Compression")]
    public static void ShowWindow()
    {
        GetWindow<AssetCompressionChecker>("Compression Checker");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Asset Compression Checker", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Scan Assets", GUILayout.Height(30)))
        {
            ScanAssets();
        }
        
        EditorGUILayout.Space();
        
        if (uncompressedTextures.Count > 0 || uncompressedAudio.Count > 0)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            if (uncompressedTextures.Count > 0)
            {
                EditorGUILayout.LabelField($"❌ TEXTURE NON COMPRESSE: {uncompressedTextures.Count}", EditorStyles.boldLabel);
                foreach (var tex in uncompressedTextures)
                {
                    if (GUILayout.Button(tex, EditorStyles.miniButton))
                    {
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(tex);
                    }
                }
                EditorGUILayout.Space();
            }
            
            if (uncompressedAudio.Count > 0)
            {
                EditorGUILayout.LabelField($"❌ AUDIO NON COMPRESSI: {uncompressedAudio.Count}", EditorStyles.boldLabel);
                foreach (var audio in uncompressedAudio)
                {
                    if (GUILayout.Button(audio, EditorStyles.miniButton))
                    {
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AudioClip>(audio);
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            if (GUILayout.Button("Fix All (Compress)", GUILayout.Height(30)))
            {
                FixAllAssets();
            }
        }
        else if (uncompressedTextures.Count == 0 && uncompressedAudio.Count == 0 && 
                 (uncompressedTextures != null || uncompressedAudio != null))
        {
            EditorGUILayout.HelpBox("✓ Tutti gli asset sono compressi correttamente!", MessageType.Info);
        }
    }
    
    void ScanAssets()
    {
        uncompressedTextures.Clear();
        uncompressedAudio.Clear();
        
        // Scan textures
        var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        foreach (var guid in textureGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null)
            {
                var androidSettings = importer.GetPlatformTextureSettings("Android");
                
                // Controlla se Android override è attivo e se usa compressione adeguata
                if (!androidSettings.overridden ||
                    androidSettings.format == TextureImporterFormat.RGBA32 ||
                    androidSettings.format == TextureImporterFormat.RGB24 ||
                    androidSettings.format == TextureImporterFormat.ARGB32)
                {
                    uncompressedTextures.Add(path);
                }
            }
        }
        
        // Scan audio
        var audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
        foreach (var guid in audioGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            
            if (importer != null)
            {
                var settings = importer.defaultSampleSettings;
                
                // Controlla se usa compressione
                if (settings.compressionFormat == AudioCompressionFormat.PCM ||
                    settings.loadType == AudioClipLoadType.DecompressOnLoad)
                {
                    uncompressedAudio.Add(path);
                }
            }
        }
        
        Debug.Log($"Scan completato: {uncompressedTextures.Count} texture non compresse, {uncompressedAudio.Count} audio non compressi");
        Repaint();
    }
    
    void FixAllAssets()
    {
        int fixedCount = 0;

        // Fix textures
        foreach (var path in uncompressedTextures)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                var androidSettings = importer.GetPlatformTextureSettings("Android");
                androidSettings.overridden = true;
                androidSettings.format = TextureImporterFormat.ASTC_6x6;
                androidSettings.maxTextureSize = 1024;

                importer.SetPlatformTextureSettings(androidSettings);
                importer.SaveAndReimport();
                fixedCount++;
            }
        }

        // Fix audio
        foreach (var path in uncompressedAudio)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;

                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
                fixedCount++;
            }
        }

        Debug.Log($"✓ Fissati {fixedCount} asset! Riesegui la scansione per verificare.");

        // Re-scan
        ScanAssets();
    }
}
