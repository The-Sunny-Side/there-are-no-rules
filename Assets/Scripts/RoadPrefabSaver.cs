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

        if (generators.Length == 0)
        {
            UnityEditor.EditorUtility.DisplayDialog("Road Prefab Saver",
                "Nessun RoadMeshGenerator trovato sotto questo oggetto.", "OK");
            return;
        }

        string mapFolder = $"Assets/Prefabs/Maps/{gameObject.name.Replace(" ", "_")}";

        EnsureFolder("Assets/Prefabs/Maps");
        EnsureFolder(mapFolder);

        string baseName = gameObject.name.Replace(" ", "_");
        int savedMeshes = 0;
        int splineOnly = 0;

        for (int i = 0; i < generators.Length; i++)
        {
            var gen = generators[i];

            // Strade spline-only (generateMesh = false): nessuna mesh da salvare,
            // è intenzionale. Fanno comunque parte del prefab della mappa.
            if (!gen.GeneratesMesh)
            {
                splineOnly++;
                continue;
            }

            Mesh sourceMesh = gen._mesh;
            if (sourceMesh == null)
                sourceMesh = gen.GetComponent<MeshFilter>()?.sharedMesh;

            if (sourceMesh == null || sourceMesh.vertexCount == 0)
            {
                Debug.LogWarning($"[RoadPrefabSaver] {gen.gameObject.name}: mesh vuota, ignorata. Esegui prima 'Generate All Roads'.");
                continue;
            }

            string meshName = $"{baseName}_{gen.gameObject.name.Replace(" ", "_")}";
            string meshPath = $"{mapFolder}/{meshName}.asset";
            Mesh meshCopy = Instantiate(sourceMesh);
            meshCopy.name = meshName;
            UnityEditor.AssetDatabase.CreateAsset(meshCopy, meshPath);

            gen._mesh = meshCopy;
            gen._meshFilter ??= gen.GetComponent<MeshFilter>();
            gen._meshCollider ??= gen.GetComponent<MeshCollider>();
            gen._meshFilter.sharedMesh = meshCopy;
            gen._meshCollider.sharedMesh = meshCopy;
            savedMeshes++;
        }

        string prefabPath = $"{mapFolder}/{baseName}.prefab";
        UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(
            gameObject, prefabPath, UnityEditor.InteractionMode.UserAction, out bool success);

        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        if (success)
            UnityEditor.EditorUtility.DisplayDialog("Road Prefab Saver",
                $"Prefab salvato: {prefabPath}\nMesh salvate: {savedMeshes} — strade spline-only: {splineOnly}", "OK");
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
