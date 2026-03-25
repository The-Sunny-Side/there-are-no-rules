using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class RoadMeshGenerator : MonoBehaviour
{
    [Header("Road Settings")]
    [SerializeField] private float roadWidth = 4f;
    [SerializeField] private int resolution = 20;
    [SerializeField] private float thickness = 1f;

    [Header("Waypoints")]
    [SerializeField] private Transform waypointsParent;

    [Header("Fork Detection")]
    [Tooltip("Se due waypoint sono a distanza simile dal corrente (differenza < delta), crea un bivio")]
    [SerializeField] private float forkDelta = 5f;

    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();
        _mesh = new Mesh { name = "RoadMesh" };
        _meshFilter.mesh = _mesh;

        var mat = new PhysicsMaterial("RoadSurface")
        {
            bounciness = 0f,
            bounceCombine = PhysicsMaterialCombine.Minimum,
            dynamicFriction = 0.6f,
            staticFriction = 0.6f,
            frictionCombine = PhysicsMaterialCombine.Average
        };
        _meshCollider.material = mat;
    }

    private void Start()
    {
        GenerateRoad();
    }

    [ContextMenu("Generate Road")]
    public void GenerateRoad()
    {
        if (waypointsParent == null || waypointsParent.childCount < 2) return;
        if (_mesh == null)
        {
            _mesh = new Mesh { name = "RoadMesh" };
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            _meshFilter.mesh = _mesh;
        }

        // Raccoglie le posizioni dei waypoint
        int count = waypointsParent.childCount;
        var positions = new Vector3[count];
        for (int i = 0; i < count; i++)
            positions[i] = waypointsParent.GetChild(i).position;

        // Costruisce i rami tramite nearest-neighbor con rilevamento bivio
        var branches = BuildBranches(positions);

        // Per ogni ramo genera i punti spline, poi combina tutto in una mesh
        var allSplinePoints = new List<List<Vector3>>();
        foreach (var branch in branches)
        {
            if (branch.Count >= 2)
                allSplinePoints.Add(GenerateSplinePoints(branch));
        }

        BuildCombinedMesh(allSplinePoints);
    }

    /// <summary>
    /// Partendo dal waypoint 0, segue il più vicino non visitato.
    /// Se due non visitati sono a distanza simile (differenza < forkDelta), crea un bivio.
    /// </summary>
    private List<List<Vector3>> BuildBranches(Vector3[] positions)
    {
        var branches = new List<List<Vector3>>();
        var visited = new HashSet<int>();

        // Coda di rami da esplorare: ogni elemento è (indice di partenza, lista punti già nel ramo)
        var queue = new Queue<(int startIdx, List<Vector3> path)>();

        var mainPath = new List<Vector3> { positions[0] };
        visited.Add(0);
        queue.Enqueue((0, mainPath));
        branches.Add(mainPath);

        while (queue.Count > 0)
        {
            var (currentIdx, currentPath) = queue.Dequeue();
            Vector3 currentPos = positions[currentIdx];

            while (true)
            {
                // Trova i due waypoint non visitati più vicini
                int nearest = -1;
                float nearestDist = float.MaxValue;
                int secondNearest = -1;
                float secondDist = float.MaxValue;

                for (int i = 0; i < positions.Length; i++)
                {
                    if (visited.Contains(i)) continue;
                    float d = Vector3.Distance(currentPos, positions[i]);

                    if (d < nearestDist)
                    {
                        secondNearest = nearest;
                        secondDist = nearestDist;
                        nearest = i;
                        nearestDist = d;
                    }
                    else if (d < secondDist)
                    {
                        secondNearest = i;
                        secondDist = d;
                    }
                }

                if (nearest == -1) break;

                // Bivio: se il secondo è a distanza simile al primo
                if (secondNearest != -1 && Mathf.Abs(nearestDist - secondDist) < forkDelta)
                {
                    // Il ramo corrente continua con nearest
                    visited.Add(nearest);
                    currentPath.Add(positions[nearest]);

                    // Nuovo ramo parte dal punto corrente verso secondNearest
                    visited.Add(secondNearest);
                    var forkPath = new List<Vector3> { currentPos, positions[secondNearest] };
                    branches.Add(forkPath);
                    queue.Enqueue((secondNearest, forkPath));

                    currentIdx = nearest;
                    currentPos = positions[nearest];
                }
                else
                {
                    visited.Add(nearest);
                    currentPath.Add(positions[nearest]);
                    currentIdx = nearest;
                    currentPos = positions[nearest];
                }
            }
        }

        return branches;
    }

    private List<Vector3> GenerateSplinePoints(List<Vector3> waypoints)
    {
        var points = new List<Vector3>();
        int count = waypoints.Count;

        for (int i = 0; i < count - 1; i++)
        {
            Vector3 p0 = waypoints[Mathf.Max(i - 1, 0)];
            Vector3 p1 = waypoints[i];
            Vector3 p2 = waypoints[Mathf.Min(i + 1, count - 1)];
            Vector3 p3 = waypoints[Mathf.Min(i + 2, count - 1)];

            int steps = (i < count - 2) ? resolution : resolution + 1;
            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)resolution;
                points.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        return points;
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    /// <summary>
    /// Combina tutti i rami in una singola mesh.
    /// </summary>
    private void BuildCombinedMesh(List<List<Vector3>> allSplinePoints)
    {
        var allVertices = new List<Vector3>();
        var allUvs = new List<Vector2>();
        var allTriangles = new List<int>();

        foreach (var splinePoints in allSplinePoints)
            BuildBranchMesh(splinePoints, allVertices, allUvs, allTriangles);

        _mesh.Clear();
        _mesh.vertices = allVertices.ToArray();
        _mesh.triangles = allTriangles.ToArray();
        _mesh.uv = allUvs.ToArray();
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        if (_meshCollider != null)
        {
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = _mesh;
        }
    }

    private void BuildBranchMesh(List<Vector3> splinePoints, List<Vector3> allVertices, List<Vector2> allUvs, List<int> allTriangles)
    {
        int pointCount = splinePoints.Count;
        if (pointCount < 2) return;

        float halfWidth = roadWidth * 0.5f;
        int vertexOffset = allVertices.Count;

        var tangents = new Vector3[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            if (i == 0)
                tangents[i] = (splinePoints[1] - splinePoints[0]).normalized;
            else if (i == pointCount - 1)
                tangents[i] = (splinePoints[i] - splinePoints[i - 1]).normalized;
            else
                tangents[i] = (splinePoints[i + 1] - splinePoints[i - 1]).normalized;
        }

        // Rotation Minimizing Frame
        var lefts = new Vector3[pointCount];
        var ups = new Vector3[pointCount];

        {
            Vector3 fwd = tangents[0];
            Vector3 left = Vector3.Cross(Vector3.up, fwd).normalized;
            if (left.sqrMagnitude < 0.001f)
                left = Vector3.Cross(Vector3.forward, fwd).normalized;
            Vector3 up = Vector3.Cross(fwd, left).normalized;
            left = Vector3.Cross(up, fwd).normalized;
            lefts[0] = left;
            ups[0] = up;
        }

        for (int i = 1; i < pointCount; i++)
        {
            Quaternion rot = Quaternion.FromToRotation(tangents[i - 1], tangents[i]);
            lefts[i] = (rot * lefts[i - 1]).normalized;
            ups[i] = (rot * ups[i - 1]).normalized;
        }

        float accumulatedLength = 0f;

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 localPos = transform.InverseTransformPoint(splinePoints[i]);
            Vector3 localLeft = transform.InverseTransformDirection(lefts[i]);
            Vector3 localUp = transform.InverseTransformDirection(ups[i]);

            Vector3 topLeft = localPos - localLeft * halfWidth;
            Vector3 topRight = localPos + localLeft * halfWidth;
            Vector3 botLeft = topLeft - localUp * thickness;
            Vector3 botRight = topRight - localUp * thickness;

            allVertices.Add(topLeft);
            allVertices.Add(topRight);
            allVertices.Add(botLeft);
            allVertices.Add(botRight);

            if (i > 0)
                accumulatedLength += Vector3.Distance(splinePoints[i], splinePoints[i - 1]);
            float v = accumulatedLength / roadWidth;
            allUvs.Add(new Vector2(0f, v));
            allUvs.Add(new Vector2(1f, v));
            allUvs.Add(new Vector2(0f, v));
            allUvs.Add(new Vector2(1f, v));
        }

        for (int i = 0; i < pointCount - 1; i++)
        {
            int c = vertexOffset + i * 4;
            int n = vertexOffset + (i + 1) * 4;

            // Top face
            allTriangles.Add(c);     allTriangles.Add(n);     allTriangles.Add(c + 1);
            allTriangles.Add(c + 1); allTriangles.Add(n);     allTriangles.Add(n + 1);

            // Bottom face
            allTriangles.Add(c + 2); allTriangles.Add(c + 3); allTriangles.Add(n + 2);
            allTriangles.Add(c + 3); allTriangles.Add(n + 3); allTriangles.Add(n + 2);

            // Left side
            allTriangles.Add(c);     allTriangles.Add(c + 2); allTriangles.Add(n);
            allTriangles.Add(n);     allTriangles.Add(c + 2); allTriangles.Add(n + 2);

            // Right side
            allTriangles.Add(c + 1); allTriangles.Add(n + 1); allTriangles.Add(c + 3);
            allTriangles.Add(c + 3); allTriangles.Add(n + 1); allTriangles.Add(n + 3);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (waypointsParent == null || waypointsParent.childCount < 2) return;

        int count = waypointsParent.childCount;
        var positions = new Vector3[count];
        for (int i = 0; i < count; i++)
            positions[i] = waypointsParent.GetChild(i).position;

        // Disegna i waypoint
        Gizmos.color = Color.yellow;
        foreach (var pos in positions)
            Gizmos.DrawWireSphere(pos, 0.3f);

        // Disegna i rami
        var branches = BuildBranches(positions);
        Color[] branchColors = { Color.cyan, Color.magenta, Color.green, Color.red, Color.blue };

        for (int b = 0; b < branches.Count; b++)
        {
            if (branches[b].Count < 2) continue;
            Gizmos.color = branchColors[b % branchColors.Length];

            var spline = GenerateSplinePoints(branches[b]);
            for (int i = 0; i < spline.Count - 1; i++)
                Gizmos.DrawLine(spline[i], spline[i + 1]);
        }
    }
#endif
}
