using UnityEngine;

public class RoadPrefabSaver : MonoBehaviour
{
#if UNITY_EDITOR
    [ContextMenu("Generate All Roads")]
    private void GenerateAll()
    {
        var generators = GetComponentsInChildren<RoadMeshGenerator>();
        foreach (var gen in generators)
            gen.GenerateRoad();

        Debug.Log($"[RoadPrefabSaver] Generate completata: {generators.Length} strade.");
    }

    [ContextMenu("Save As Prefab")]
    private void SaveAsPrefab()
    {
        var generators = GetComponentsInChildren<RoadMeshGenerator>();

        string mapFolder = $"Assets/Prefabs/Maps/{gameObject.name.Replace(" ", "_")}";

        EnsureFolder("Assets/Prefabs/Maps");
        EnsureFolder(mapFolder);

        string baseName = gameObject.name.Replace(" ", "_");
        int saved = 0;

        for (int i = 0; i < generators.Length; i++)
        {
            var gen = generators[i];
            if (gen._mesh == null || gen._mesh.vertexCount == 0)
            {
                Debug.LogWarning($"[RoadPrefabSaver] {gen.gameObject.name}: mesh vuota, ignorata. Esegui prima 'Generate All Roads'.");
                continue;
            }

            string meshName = $"{baseName}_{gen.gameObject.name.Replace(" ", "_")}";
            string meshPath = $"{mapFolder}/{meshName}.asset";
            Mesh meshCopy = Instantiate(gen._mesh);
            meshCopy.name = meshName;
            UnityEditor.AssetDatabase.CreateAsset(meshCopy, meshPath);

            gen._meshFilter.sharedMesh = meshCopy;
            gen._meshCollider.sharedMesh = meshCopy;
            saved++;
        }

        if (saved == 0)
        {
            UnityEditor.EditorUtility.DisplayDialog("Road Prefab Saver",
                "Nessuna mesh trovata.\nEsegui prima 'Generate All Roads'.", "OK");
            return;
        }

        string prefabPath = $"{mapFolder}/{baseName}.prefab";
        UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(
            gameObject, prefabPath, UnityEditor.InteractionMode.UserAction, out bool success);

        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        if (success)
            UnityEditor.EditorUtility.DisplayDialog("Road Prefab Saver",
                $"{saved} mesh salvate.\nPrefab: {prefabPath}", "OK");
        else
            UnityEditor.EditorUtility.DisplayDialog("Road Prefab Saver",
                "Errore durante il salvataggio del prefab.", "OK");
    }

    private static void EnsureFolder(string path)
    {
        if (!UnityEditor.AssetDatabase.IsValidFolder(path))
        {
            int lastSlash = path.LastIndexOf('/');
            UnityEditor.AssetDatabase.CreateFolder(path[..lastSlash], path[(lastSlash + 1)..]);
        }
    }
#endif
}
