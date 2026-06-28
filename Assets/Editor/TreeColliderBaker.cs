using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera SOLO i collider (leggeri) alle posizioni degli alberi/rocce piantati col Paint Trees,
/// copiando il collider del prefab (Capsule/Mesh/Box/Sphere) col suo physic material (es. bouncy).
/// I visual restano quelli nativi del Terrain: qui non si istanzia nessuna mesh.
///
/// Apri: Tools > Trees > Tree Collider Baker. Seleziona il Terrain, poi Bake.
/// Poi disattiva "Enable Tree Colliders" sul Terrain Collider (per non avere collider doppi).
/// </summary>
public class TreeColliderBaker : EditorWindow
{
    private const string ContainerName = "BakedTreeColliders";
    private float scaleMultiplier = 1f;

    [MenuItem("Tools/Trees/Tree Collider Baker")]
    private static void Open() => GetWindow<TreeColliderBaker>("Tree Collider Baker");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "1) Seleziona il GameObject col Terrain.\n2) Bake.\n3) Disattiva 'Enable Tree Colliders' sul Terrain Collider.\n\n" +
            "Se i collider risultano troppo grandi/piccoli rispetto agli alberi, regola lo Scale Multiplier e rifai il Bake.",
            MessageType.Info);

        scaleMultiplier = EditorGUILayout.FloatField("Scale Multiplier", scaleMultiplier);

        EditorGUILayout.Space();
        if (GUILayout.Button("Bake Colliders From Selected Terrain")) Bake();
        if (GUILayout.Button("Clear Baked Colliders")) Clear();
    }

    private void Bake()
    {
        var terrain = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Terrain>() : null;
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Trees", "Seleziona un GameObject con un componente Terrain.", "OK");
            return;
        }

        var data = terrain.terrainData;
        var protos = data.treePrototypes;

        // Primo collider di ogni prototipo (cerca anche nei figli LOD).
        var srcColliders = new Collider[protos.Length];
        for (int i = 0; i < protos.Length; i++)
            srcColliders[i] = protos[i].prefab ? protos[i].prefab.GetComponentInChildren<Collider>(true) : null;

        // Container come figlio del Terrain (per ordine). Compenseremo la scala ereditata
        // sui singoli collider, così non risultano deformati dalla scala del Terrain/genitori.
        var old = terrain.transform.Find(ContainerName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var container = new GameObject(ContainerName);
        Undo.RegisterCreatedObjectUndo(container, "Bake Tree Colliders");
        container.transform.SetParent(terrain.transform, false);

        Vector3 parentLossy = container.transform.lossyScale; // scala-mondo ereditata dal Terrain
        Vector3 size = data.size;
        var instances = data.treeInstances;
        int created = 0, skipped = 0;
        bool logged = false;

        foreach (var t in instances)
        {
            var src = srcColliders[t.prototypeIndex];
            if (src == null) { skipped++; continue; }

            var go = new GameObject("TreeCol");
            go.transform.SetParent(container.transform, true);

            // posizione: formula canonica Unity per gli alberi del Terrain
            go.transform.position = terrain.transform.position + Vector3.Scale(t.position, size);
            go.transform.rotation = Quaternion.Euler(0f, t.rotation * Mathf.Rad2Deg, 0f);

            // scala-mondo voluta: collider del prefab (lossyScale) * scala istanza * multiplier
            Vector3 desiredWorld =
                Vector3.Scale(src.transform.lossyScale, new Vector3(t.widthScale, t.heightScale, t.widthScale))
                * scaleMultiplier;
            // la dividiamo per la scala ereditata dal container, così la scala-mondo resta quella voluta
            go.transform.localScale = new Vector3(
                desiredWorld.x / Safe(parentLossy.x),
                desiredWorld.y / Safe(parentLossy.y),
                desiredWorld.z / Safe(parentLossy.z));

            go.isStatic = true;
            CopyCollider(src, go);
            created++;

            if (!logged)
            {
                Debug.Log($"[TreeColliderBaker] DIAG primo collider: proto={t.prototypeIndex} " +
                          $"tipo={src.GetType().Name} src.lossyScale={src.transform.lossyScale} " +
                          $"istanza=(w {t.widthScale}, h {t.heightScale}) multiplier={scaleMultiplier} " +
                          $"parentLossy={parentLossy} -> scala-mondo={desiredWorld} (local={go.transform.localScale})", go);
                logged = true;
            }
        }

        Debug.Log($"[TreeColliderBaker] Creati {created} collider in '{ContainerName}'" +
                  (skipped > 0 ? $" ({skipped} saltati: prototipo senza collider)." : ".") +
                  " Ora disattiva 'Enable Tree Colliders' sul Terrain Collider.", container);
        Selection.activeGameObject = container;
    }

    private void Clear()
    {
        var terrain = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Terrain>() : null;
        Transform found = terrain != null ? terrain.transform.Find(ContainerName) : null;
        if (found == null)
        {
            var atRoot = GameObject.Find(ContainerName); // eventuali vecchi container al root
            if (atRoot != null) found = atRoot.transform;
        }
        if (found != null) Undo.DestroyObjectImmediate(found.gameObject);
    }

    private static float Safe(float v) => Mathf.Approximately(v, 0f) ? 1f : v;

    private static void CopyCollider(Collider src, GameObject target)
    {
        switch (src)
        {
            case CapsuleCollider c:
                var nc = target.AddComponent<CapsuleCollider>();
                nc.center = c.center; nc.radius = c.radius; nc.height = c.height;
                nc.direction = c.direction; nc.sharedMaterial = c.sharedMaterial;
                break;
            case MeshCollider m:
                var nm = target.AddComponent<MeshCollider>();
                nm.sharedMesh = m.sharedMesh; nm.convex = m.convex; nm.sharedMaterial = m.sharedMaterial;
                break;
            case BoxCollider b:
                var nb = target.AddComponent<BoxCollider>();
                nb.center = b.center; nb.size = b.size; nb.sharedMaterial = b.sharedMaterial;
                break;
            case SphereCollider s:
                var ns = target.AddComponent<SphereCollider>();
                ns.center = s.center; ns.radius = s.radius; ns.sharedMaterial = s.sharedMaterial;
                break;
        }
    }
}
