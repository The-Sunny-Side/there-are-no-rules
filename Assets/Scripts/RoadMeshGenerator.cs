using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class RoadMeshGenerator : MonoBehaviour
{
    [Header("Road Settings")]
    [SerializeField] private float roadWidth = 4f;
    [SerializeField] private int resolution = 20;
    [SerializeField] private float thickness = 1f;
    [Header("Edge Curve (U-shape)")]
    [Tooltip("Altezza dei bordi rialzati. 0 = strada piatta")]
    [SerializeField] private float edgeCurveHeight = 0.5f;
    [Tooltip("Segmenti trasversali per la curvatura")]
    [SerializeField, Range(2, 16)] private int widthSegments = 8;

    [Header("Kill Zone")]
    [Tooltip("Quanto sotto il centro della strada viene piazzata la rete di sicurezza")]
    [SerializeField] private float killZoneDistanceBelow = 5f;
    [Tooltip("Larghezza totale della rete di sicurezza in unità")]
    [SerializeField] private float killZoneWidth = 20f;

    [Header("Waypoints")]
    [SerializeField] private Transform waypointsParent;

    [NonSerialized] public Mesh _mesh;
    [NonSerialized] public MeshFilter _meshFilter;
    [NonSerialized] public MeshCollider _meshCollider;

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

        var waypoints = new List<Vector3>();
        var nodeTypes = new List<bool>(); // true = Flat
        var stopNodes = new List<bool>(); // true = Stop (no mesh on outgoing arc)
        var waypointBankAngles = new List<float>();
        var waypointTensions = new List<float>();
        for (int i = 0; i < waypointsParent.childCount; i++)
        {
            Transform child = waypointsParent.GetChild(i);
            waypoints.Add(child.position);
            var node = child.GetComponent<WaypointNode>();
            nodeTypes.Add(node != null && node.nodeType == WaypointNode.NodeType.Flat);
            stopNodes.Add(node != null && node.nodeType == WaypointNode.NodeType.Stop);
            waypointBankAngles.Add(node != null ? node.bankAngle : 0f);
            waypointTensions.Add(node != null && node.overrideTension ? node.splineTension : 0f);
        }

        var splineData = GenerateSplinePoints(waypoints, nodeTypes, waypointBankAngles, stopNodes, waypointTensions);
        BuildMesh(splineData.points, splineData.flatWeights, splineData.bankAngles, splineData.meshActive);
        BuildKillZoneNet(splineData.points, splineData.meshActive);
    }

    private (List<Vector3> points, List<float> flatWeights, List<float> bankAngles, List<bool> meshActive) GenerateSplinePoints(
        List<Vector3> waypoints, List<bool> nodeTypes, List<float> waypointBankAngles, List<bool> stopNodes, List<float> waypointTensions)
    {
        var points = new List<Vector3>();
        var flatWeights = new List<float>();
        var bankAngles = new List<float>();
        var meshActive = new List<bool>(); // per-point: false = non generare mesh verso il punto successivo
        int count = waypoints.Count;

        for (int i = 0; i < count - 1; i++)
        {
            Vector3 p0 = waypoints[Mathf.Max(i - 1, 0)];
            Vector3 p1 = waypoints[i];
            Vector3 p2 = waypoints[Mathf.Min(i + 1, count - 1)];
            Vector3 p3 = waypoints[Mathf.Min(i + 2, count - 1)];

            float flatA = nodeTypes[i] ? 1f : 0f;
            float flatB = nodeTypes[Mathf.Min(i + 1, count - 1)] ? 1f : 0f;

            float bankA = waypointBankAngles[i];
            float bankB = waypointBankAngles[Mathf.Min(i + 1, count - 1)];

            float tensionA = waypointTensions[i];
            float tensionB = waypointTensions[Mathf.Min(i + 1, count - 1)];

            bool generateMesh = !stopNodes[i];

            int steps = (i < count - 2) ? resolution : resolution + 1;
            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)resolution;
                float tension = Mathf.Lerp(tensionA, tensionB, t);
                points.Add(CatmullRom(p0, p1, p2, p3, t, tension));
                flatWeights.Add(Mathf.Lerp(flatA, flatB, t));
                bankAngles.Add(Mathf.Lerp(bankA, bankB, t));
                meshActive.Add(generateMesh);
            }
        }

        return (points, flatWeights, bankAngles, meshActive);
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float tension)
    {
        float s = (1f - tension) / 2f;
        float t2 = t * t;
        float t3 = t2 * t;

        float h1 = 2f * t3 - 3f * t2 + 1f;
        float h2 = t3 - 2f * t2 + t;
        float h3 = -2f * t3 + 3f * t2;
        float h4 = t3 - t2;

        return h1 * p1 + h2 * s * (p2 - p0) + h3 * p2 + h4 * s * (p3 - p1);
    }

    private void BuildMesh(List<Vector3> splinePoints, List<float> flatWeights, List<float> bankAngles, List<bool> meshActive)
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

        // Frame ibrido per-punto:
        // flatWeight=0 (Default) → Rotation Minimizing Frame (smooth curve)
        // flatWeight=1 (Flat)    → Vector3.up (piatto, per bivi)
        // Valori intermedi blendano tra i due
        var lefts = new Vector3[pointCount];
        var ups = new Vector3[pointCount];

        // RMF: primo punto
        Vector3 rmfLeft, rmfUp;
        {
            Vector3 fwd = tangents[0];
            rmfLeft = Vector3.Cross(Vector3.up, fwd).normalized;
            if (rmfLeft.sqrMagnitude < 0.001f)
                rmfLeft = Vector3.Cross(Vector3.forward, fwd).normalized;
            rmfUp = Vector3.Cross(fwd, rmfLeft).normalized;
            rmfLeft = Vector3.Cross(rmfUp, fwd).normalized;
        }

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 fwd = tangents[i];

            // Flat frame (da Vector3.up)
            Vector3 flatLeft = Vector3.Cross(Vector3.up, fwd).normalized;
            if (flatLeft.sqrMagnitude < 0.001f)
                flatLeft = Vector3.Cross(Vector3.forward, fwd).normalized;
            Vector3 flatUp = Vector3.Cross(fwd, flatLeft).normalized;
            flatLeft = Vector3.Cross(flatUp, fwd).normalized;

            // RMF: propaga dal punto precedente
            if (i > 0)
            {
                Quaternion rot = Quaternion.FromToRotation(tangents[i - 1], fwd);
                rmfLeft = (rot * rmfLeft).normalized;
                rmfUp = (rot * rmfUp).normalized;
            }

            // Blend in base al flatWeight
            float w = flatWeights[i];
            lefts[i] = Vector3.Slerp(rmfLeft, flatLeft, w).normalized;
            ups[i] = Vector3.Slerp(rmfUp, flatUp, w).normalized;

            // Applica banking: ruota left/up attorno alla tangente
            float bank = bankAngles[i];
            if (Mathf.Abs(bank) > 0.01f)
            {
                Quaternion bankRot = Quaternion.AngleAxis(bank, fwd);
                lefts[i] = (bankRot * lefts[i]).normalized;
                ups[i] = (bankRot * ups[i]).normalized;
            }
        }

        int segs = Mathf.Max(2, widthSegments);
        int vertsPerRow = segs + 1;
        int vertsPerPoint = vertsPerRow * 2; // top row + bottom row
        int vertCount = pointCount * vertsPerPoint;

        var vertices = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        var trianglesList = new List<int>();
        float accumulatedLength = 0f;

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 localPos = transform.InverseTransformPoint(splinePoints[i]);
            Vector3 localLeft = transform.InverseTransformDirection(lefts[i]);
            Vector3 localUp = transform.InverseTransformDirection(ups[i]);

            if (i > 0)
                accumulatedLength += Vector3.Distance(splinePoints[i], splinePoints[i - 1]);
            float v = accumulatedLength / roadWidth;

            int baseIdx = i * vertsPerPoint;

            for (int j = 0; j <= segs; j++)
            {
                float u = (float)j / segs;              // 0..1
                float centered = u * 2f - 1f;           // -1..+1

                // Posizione trasversale
                Vector3 widthOffset = localLeft * (centered * halfWidth);

                // Curva a U: parabolica, 0 al centro, edgeCurveHeight ai bordi
                // Si appiattisce ai nodi Flat (bivi/incroci) per far combaciare le strade
                float curveOffset = edgeCurveHeight * (1f - flatWeights[i]) * centered * centered;
                Vector3 heightOffset = localUp * curveOffset;

                Vector3 topPos = localPos + widthOffset + heightOffset;
                Vector3 botPos = topPos - localUp * thickness;

                vertices[baseIdx + j] = topPos;
                vertices[baseIdx + vertsPerRow + j] = botPos;

                uvs[baseIdx + j] = new Vector2(u, v);
                uvs[baseIdx + vertsPerRow + j] = new Vector2(u, v);
            }
        }

        for (int i = 0; i < pointCount - 1; i++)
        {
            if (!meshActive[i]) continue;

            int c = i * vertsPerPoint;
            int n = (i + 1) * vertsPerPoint;

            // Top face — strip di quads
            for (int j = 0; j < segs; j++)
            {
                trianglesList.Add(c + j);
                trianglesList.Add(n + j);
                trianglesList.Add(c + j + 1);

                trianglesList.Add(c + j + 1);
                trianglesList.Add(n + j);
                trianglesList.Add(n + j + 1);
            }

            // Bottom face — strip di quads (winding invertito)
            int cb = c + vertsPerRow;
            int nb = n + vertsPerRow;
            for (int j = 0; j < segs; j++)
            {
                trianglesList.Add(cb + j);
                trianglesList.Add(cb + j + 1);
                trianglesList.Add(nb + j);

                trianglesList.Add(cb + j + 1);
                trianglesList.Add(nb + j + 1);
                trianglesList.Add(nb + j);
            }

            // Left side
            trianglesList.Add(c);
            trianglesList.Add(c + vertsPerRow);
            trianglesList.Add(n);

            trianglesList.Add(n);
            trianglesList.Add(c + vertsPerRow);
            trianglesList.Add(n + vertsPerRow);

            // Right side
            trianglesList.Add(c + segs);
            trianglesList.Add(n + segs);
            trianglesList.Add(c + vertsPerRow + segs);

            trianglesList.Add(c + vertsPerRow + segs);
            trianglesList.Add(n + segs);
            trianglesList.Add(n + vertsPerRow + segs);
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = trianglesList.ToArray();
        _mesh.uv = uvs;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        if (_meshCollider != null)
        {
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = _mesh;
        }
    }

    private void BuildKillZoneNet(List<Vector3> splinePoints, List<bool> meshActive)
    {
        const string netName = "KillZoneNet";
        var existing = transform.Find(netName);
        if (existing != null) DestroyImmediate(existing.gameObject);

        var netGO = new GameObject(netName);
        netGO.transform.SetParent(transform);
        netGO.transform.localPosition = Vector3.zero;
        netGO.transform.localRotation = Quaternion.identity;
        netGO.transform.localScale = Vector3.one;

        int pointCount = splinePoints.Count;

        for (int i = 0; i < pointCount - 1; i++)
        {
            if (!meshActive[i]) continue;

            Vector3 a = splinePoints[i];
            Vector3 b = splinePoints[i + 1];
            float segLength = Vector3.Distance(a, b);
            if (segLength < 0.001f) continue;

            Vector3 mid = (a + b) * 0.5f + Vector3.down * killZoneDistanceBelow;
            Vector3 dir = (b - a).normalized;

            var segGO = new GameObject($"KZSeg_{i}");
            segGO.transform.SetParent(netGO.transform);
            segGO.transform.position = mid;
            segGO.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            var bc = segGO.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(killZoneWidth, 2f, segLength + 0.1f);

            segGO.AddComponent<KillZoneTrigger>();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (waypointsParent == null || waypointsParent.childCount < 2) return;

        var waypoints = new List<Vector3>();
        var nodeTypes = new List<bool>();
        var stopNodes = new List<bool>();
        var waypointBankAngles = new List<float>();
        var waypointTensions = new List<float>();
        for (int i = 0; i < waypointsParent.childCount; i++)
        {
            Transform child = waypointsParent.GetChild(i);
            waypoints.Add(child.position);
            var node = child.GetComponent<WaypointNode>();
            nodeTypes.Add(node != null && node.nodeType == WaypointNode.NodeType.Flat);
            stopNodes.Add(node != null && node.nodeType == WaypointNode.NodeType.Stop);
            waypointBankAngles.Add(node != null ? node.bankAngle : 0f);
            waypointTensions.Add(node != null && node.overrideTension ? node.splineTension : 0f);
        }

        // Waypoint: giallo = Default, verde = Flat, rosso = Stop
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (stopNodes[i])
                Gizmos.color = Color.red;
            else
                Gizmos.color = nodeTypes[i] ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(waypoints[i], 0.3f);
        }

        var splineData = GenerateSplinePoints(waypoints, nodeTypes, waypointBankAngles, stopNodes, waypointTensions);
        for (int i = 0; i < splineData.points.Count - 1; i++)
        {
            Gizmos.color = splineData.meshActive[i] ? Color.cyan : Color.red;
            Gizmos.DrawLine(splineData.points[i], splineData.points[i + 1]);
        }
    }
#endif
}
