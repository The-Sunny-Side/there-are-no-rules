using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)] // Esegue dopo RoadMeshGenerator (default = 0)
public class RoadOverlapCleaner : MonoBehaviour
{
    [Tooltip("Dimensione cella per spatial hashing (circa la dimensione di un triangolo)")]
    [SerializeField] private float cellSize = 1f;

    [Tooltip("Differenza massima in Y per considerare due facce sovrapposte")]
    [SerializeField] private float maxHeightDiff = 1f;

    [Tooltip("Gap minimo tra indici per self-overlap nella stessa mesh")]
    [SerializeField] private int selfOverlapMinIndexGap = 500;

    private void Start()
    {
        // Le strade sono già generate dai RoadMeshGenerator.Start() (execution order 0)
        CleanOverlaps(false);
    }

    [ContextMenu("Clean Overlaps")]
    public void CleanOverlapsFromMenu()
    {
        CleanOverlaps(true);
    }

    public void CleanOverlaps(bool regenerate = true)
    {
        var generators = GetComponentsInChildren<RoadMeshGenerator>();
        if (regenerate)
        {
            foreach (var gen in generators)
                gen.GenerateRoad();
        }

        var meshFilters = new List<MeshFilter>();
        foreach (var gen in generators)
        {
            var mf = gen.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null && mf.sharedMesh.triangles.Length > 0)
                meshFilters.Add(mf);
        }

        if (meshFilters.Count == 0) return;

        // Raccoglie tutti i triangoli in world space con proiezione XZ
        var allTris = new List<TriData>();
        var grid = new Dictionary<Vector2Int, List<int>>();

        for (int m = 0; m < meshFilters.Count; m++)
        {
            var mf = meshFilters[m];
            var mesh = mf.sharedMesh;
            var verts = mesh.vertices;
            var tris = mesh.triangles;
            var xform = mf.transform;

            for (int t = 0; t < tris.Length; t += 3)
            {
                Vector3 w0 = xform.TransformPoint(verts[tris[t]]);
                Vector3 w1 = xform.TransformPoint(verts[tris[t + 1]]);
                Vector3 w2 = xform.TransformPoint(verts[tris[t + 2]]);

                Vector3 center = (w0 + w1 + w2) / 3f;

                int idx = allTris.Count;
                allTris.Add(new TriData
                {
                    a = new Vector2(w0.x, w0.z),
                    b = new Vector2(w1.x, w1.z),
                    c = new Vector2(w2.x, w2.z),
                    centerY = center.y,
                    meshIdx = m,
                    triIdx = t
                });

                // Inserisce nel grid la cella del centro + celle coperte dai vertici
                InsertInGrid(grid, w0, idx);
                InsertInGrid(grid, w1, idx);
                InsertInGrid(grid, w2, idx);
                InsertInGrid(grid, center, idx);
            }
        }

        // Per ogni triangolo, controlla se TUTTI E 3 i vertici sono coperti da triangoli dell'altra mesh
        var removals = new List<HashSet<int>>();
        for (int m = 0; m < meshFilters.Count; m++)
            removals.Add(new HashSet<int>());

        for (int i = 0; i < allTris.Count; i++)
        {
            var tri = allTris[i];
            if (removals[tri.meshIdx].Contains(tri.triIdx)) continue;

            Vector2[] verts2d = { tri.a, tri.b, tri.c };
            bool[] covered = { false, false, false };

            // Per ogni vertice, cerca nelle celle vicine se è dentro un triangolo dell'altra mesh
            for (int v = 0; v < 3; v++)
            {
                var vCell = new Vector2Int(
                    Mathf.FloorToInt(verts2d[v].x / cellSize),
                    Mathf.FloorToInt(verts2d[v].y / cellSize)
                );

                for (int dx = -1; dx <= 1 && !covered[v]; dx++)
                for (int dz = -1; dz <= 1 && !covered[v]; dz++)
                {
                    var nCell = new Vector2Int(vCell.x + dx, vCell.y + dz);
                    if (!grid.TryGetValue(nCell, out var indices)) continue;

                    for (int j = 0; j < indices.Count; j++)
                    {
                        int otherIdx = indices[j];
                        if (otherIdx >= i) continue;

                        var other = allTris[otherIdx];
                        if (removals[other.meshIdx].Contains(other.triIdx)) continue;

                        if (other.meshIdx == tri.meshIdx &&
                            Mathf.Abs(other.triIdx - tri.triIdx) < selfOverlapMinIndexGap)
                            continue;

                        if (Mathf.Abs(tri.centerY - other.centerY) > maxHeightDiff)
                            continue;

                        if (PointInTriangle2D(verts2d[v], other.a, other.b, other.c))
                        {
                            covered[v] = true;
                            break;
                        }
                    }
                }

                // Se un vertice non è coperto, il triangolo non è completamente dentro → skip
                if (!covered[v]) break;
            }

            if (covered[0] && covered[1] && covered[2])
                removals[tri.meshIdx].Add(tri.triIdx);
        }

        // Rebuild meshes
        int totalRemoved = 0;
        for (int m = 0; m < meshFilters.Count; m++)
        {
            if (removals[m].Count == 0) continue;

            var mesh = meshFilters[m].sharedMesh;
            var tris = mesh.triangles;

            var newTris = new List<int>(tris.Length);
            for (int t = 0; t < tris.Length; t += 3)
            {
                if (!removals[m].Contains(t))
                {
                    newTris.Add(tris[t]);
                    newTris.Add(tris[t + 1]);
                    newTris.Add(tris[t + 2]);
                }
            }

            totalRemoved += removals[m].Count;

            mesh.triangles = newTris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var collider = meshFilters[m].GetComponent<MeshCollider>();
            if (collider != null)
            {
                collider.sharedMesh = null;
                collider.sharedMesh = mesh;
            }
        }

        Debug.Log($"[RoadOverlapCleaner] Rimossi {totalRemoved} triangoli sovrapposti da {meshFilters.Count} mesh");
    }

    private void InsertInGrid(Dictionary<Vector2Int, List<int>> grid, Vector3 worldPos, int idx)
    {
        var cell = new Vector2Int(
            Mathf.FloorToInt(worldPos.x / cellSize),
            Mathf.FloorToInt(worldPos.z / cellSize)
        );
        if (!grid.ContainsKey(cell))
            grid[cell] = new List<int>();
        if (!grid[cell].Contains(idx))
            grid[cell].Add(idx);
    }

    private static bool PointInTriangle2D(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross2D(p, a, b);
        float d2 = Cross2D(p, b, c);
        float d3 = Cross2D(p, c, a);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float Cross2D(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private struct TriData
    {
        public Vector2 a, b, c;
        public float centerY;
        public int meshIdx;
        public int triIdx;
    }
}
