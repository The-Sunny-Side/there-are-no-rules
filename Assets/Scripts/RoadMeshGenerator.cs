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

        var branches = CollectBranches();

        var allSplinePoints = new List<List<Vector3>>();
        foreach (var branch in branches)
        {
            if (branch.Count >= 2)
                allSplinePoints.Add(GenerateSplinePoints(branch));
        }

        BuildCombinedMesh(allSplinePoints);
    }

    /// <summary>
    /// Raccoglie i rami dalla gerarchia:
    /// - Figli diretti del parent = strada principale (in ordine)
    /// - Se un figlio ha sotto-figli con gruppi = bivio
    ///   Ogni gruppo (figlio del fork node) è un ramo indipendente
    ///   Se ci sono waypoint dopo il fork nella principale, ogni ramo riconverge lì
    /// </summary>
    private List<List<Vector3>> CollectBranches()
    {
        var branches = new List<List<Vector3>>();
        var mainPath = new List<Vector3>();

        for (int i = 0; i < waypointsParent.childCount; i++)
        {
            Transform child = waypointsParent.GetChild(i);

            // Nodo fork: ha figli che sono gruppi (ogni gruppo ha a sua volta figli waypoint)
            if (child.childCount > 0 && child.GetChild(0).childCount > 0)
            {
                // Il fork point è parte della strada principale
                mainPath.Add(child.position);

                // Raccoglie i waypoint dopo il fork per la riconvergenza
                var afterFork = new List<Vector3>();
                for (int k = i + 1; k < waypointsParent.childCount; k++)
                    afterFork.Add(waypointsParent.GetChild(k).position);

                // Ogni figlio del fork node è un gruppo/ramo
                for (int g = 0; g < child.childCount; g++)
                {
                    Transform group = child.GetChild(g);
                    var forkPath = new List<Vector3>();

                    // Parte dal punto di fork
                    forkPath.Add(child.position);

                    // Aggiunge i waypoint del gruppo
                    for (int j = 0; j < group.childCount; j++)
                        forkPath.Add(group.GetChild(j).position);

                    // Riconverge sulla principale (se ci sono waypoint dopo)
                    if (afterFork.Count > 0)
                        forkPath.AddRange(afterFork);

                    branches.Add(forkPath);
                }

                // La strada principale salta il fork (i rami la coprono)
                // Continua dal prossimo waypoint normale
            }
            // Nodo fork semplice: ha figli diretti (non gruppi) = singolo ramo
            else if (child.childCount > 0)
            {
                mainPath.Add(child.position);

                var forkPath = new List<Vector3>();
                forkPath.Add(child.position);

                for (int j = 0; j < child.childCount; j++)
                    forkPath.Add(child.GetChild(j).position);

                // Riconverge
                for (int k = i + 1; k < waypointsParent.childCount; k++)
                    forkPath.Add(waypointsParent.GetChild(k).position);

                branches.Add(forkPath);
            }
            else
            {
                mainPath.Add(child.position);
            }
        }

        branches.Insert(0, mainPath);
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

        var branches = CollectBranches();
        Color[] branchColors = { Color.cyan, Color.magenta, Color.green, Color.red, Color.blue };

        for (int b = 0; b < branches.Count; b++)
        {
            if (branches[b].Count < 2) continue;
            Gizmos.color = branchColors[b % branchColors.Length];

            // Disegna waypoint del ramo
            foreach (var pos in branches[b])
                Gizmos.DrawWireSphere(pos, 0.3f);

            // Disegna spline
            var spline = GenerateSplinePoints(branches[b]);
            for (int i = 0; i < spline.Count - 1; i++)
                Gizmos.DrawLine(spline[i], spline[i + 1]);
        }
    }
#endif
}
