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

    private Transform[] waypoints;
    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();
        _mesh = new Mesh { name = "RoadMesh" };
        _meshFilter.mesh = _mesh;

        // Zero rimbalzo sulla strada: il veicolo non deve "saltellare" sui bordi dei triangoli
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

    private void CollectWaypoints()
    {
        if (waypointsParent == null) return;
        int count = waypointsParent.childCount;
        waypoints = new Transform[count];
        for (int i = 0; i < count; i++)
            waypoints[i] = waypointsParent.GetChild(i);
    }

    [ContextMenu("Generate Road")]
    public void GenerateRoad()
    {
        CollectWaypoints();
        if (waypoints == null || waypoints.Length < 2) return;
        if (_mesh == null)
        {
            _mesh = new Mesh { name = "RoadMesh" };
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            _meshFilter.mesh = _mesh;
        }

        List<Vector3> splinePoints = GenerateSplinePoints();
        BuildMesh(splinePoints);
    }

    /// <summary>
    /// Catmull-Rom spline: genera punti interpolati tra i waypoint per curve morbide.
    /// </summary>
    private List<Vector3> GenerateSplinePoints()
    {
        var points = new List<Vector3>();
        int count = waypoints.Length;

        for (int i = 0; i < count - 1; i++)
        {
            Vector3 p0 = waypoints[Mathf.Max(i - 1, 0)].position;
            Vector3 p1 = waypoints[i].position;
            Vector3 p2 = waypoints[Mathf.Min(i + 1, count - 1)].position;
            Vector3 p3 = waypoints[Mathf.Min(i + 2, count - 1)].position;

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

    private void BuildMesh(List<Vector3> splinePoints)
    {
        int pointCount = splinePoints.Count;
        float halfWidth = roadWidth * 0.5f;

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

        // Rotation Minimizing Frame: propaga il frame dal primo punto in avanti.
        // A differenza del Frenet frame, non degenera su pendenze ripide
        // perché non dipende da Vector3.up per ogni punto.
        var lefts = new Vector3[pointCount];
        var ups = new Vector3[pointCount];

        // Primo punto: calcola con Vector3.up (funziona se il primo tratto non è verticale)
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

        // Propaga il frame: ruota il left/up del punto precedente verso la nuova tangente
        for (int i = 1; i < pointCount; i++)
        {
            Vector3 prevFwd = tangents[i - 1];
            Vector3 currFwd = tangents[i];

            // Rotazione che porta prevFwd su currFwd
            Quaternion rot = Quaternion.FromToRotation(prevFwd, currFwd);

            // Applica la stessa rotazione al frame precedente
            lefts[i] = (rot * lefts[i - 1]).normalized;
            ups[i] = (rot * ups[i - 1]).normalized;
        }

        int vertCount = pointCount * 4;
        int triCount = (pointCount - 1) * 24;
        var vertices = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        var triangles = new int[triCount];
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

            int vi = i * 4;
            vertices[vi] = topLeft;
            vertices[vi + 1] = topRight;
            vertices[vi + 2] = botLeft;
            vertices[vi + 3] = botRight;

            if (i > 0)
                accumulatedLength += Vector3.Distance(splinePoints[i], splinePoints[i - 1]);
            float v = accumulatedLength / roadWidth;
            uvs[vi] = new Vector2(0f, v);
            uvs[vi + 1] = new Vector2(1f, v);
            uvs[vi + 2] = new Vector2(0f, v);
            uvs[vi + 3] = new Vector2(1f, v);
        }

        int ti = 0;
        for (int i = 0; i < pointCount - 1; i++)
        {
            int c = i * 4;
            int n = (i + 1) * 4;

            // Top face
            triangles[ti++] = c;     triangles[ti++] = n;     triangles[ti++] = c + 1;
            triangles[ti++] = c + 1; triangles[ti++] = n;     triangles[ti++] = n + 1;

            // Bottom face
            triangles[ti++] = c + 2; triangles[ti++] = c + 3; triangles[ti++] = n + 2;
            triangles[ti++] = c + 3; triangles[ti++] = n + 3; triangles[ti++] = n + 2;

            // Left side
            triangles[ti++] = c;     triangles[ti++] = c + 2; triangles[ti++] = n;
            triangles[ti++] = n;     triangles[ti++] = c + 2; triangles[ti++] = n + 2;

            // Right side
            triangles[ti++] = c + 1; triangles[ti++] = n + 1; triangles[ti++] = c + 3;
            triangles[ti++] = c + 3; triangles[ti++] = n + 1; triangles[ti++] = n + 3;
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.uv = uvs;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        if (_meshCollider != null)
        {
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = _mesh;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.yellow;
        foreach (var wp in waypoints)
        {
            if (wp != null)
                Gizmos.DrawWireSphere(wp.position, 0.3f);
        }

        Gizmos.color = Color.cyan;
        List<Vector3> points = GenerateSplinePoints();
        for (int i = 0; i < points.Count - 1; i++)
            Gizmos.DrawLine(points[i], points[i + 1]);
    }
#endif
}
