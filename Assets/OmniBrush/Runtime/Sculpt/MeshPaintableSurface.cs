using System.Collections.Generic;
using UnityEngine;

namespace OmniBrush
{
    /// <summary>
    /// Mesh implementation of IPaintableSurface: sculpts any MeshFilter
    /// (sphere, plane, imported model...). The shared mesh asset is never
    /// touched — the first stamp clones it (see MeshDeformation). CPU
    /// vertex ops for now; a GPU delta-map path is planned for high-poly.
    /// </summary>
    public class MeshPaintableSurface : IPaintableSurface
    {
        public delegate void MeshRecordHook(Mesh mesh, int[] indices, Vector3[] before, Vector3[] after);

        /// <summary>Editor hook: receives per-stamp vertex diffs for undo.</summary>
        public static MeshRecordHook recordHook;

        private const float RaiseStepFraction = 0.02f; // of radius, per full-strength stamp

        private readonly MeshFilter filter;

        public MeshPaintableSurface(MeshFilter filter) => this.filter = filter;

        public Object Target => filter;
        public MeshFilter Filter => filter;

        public static MeshPaintableSurface TryFrom(Collider collider)
        {
            if (collider == null || collider is TerrainCollider) return null;
            MeshFilter mf = collider.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) return new MeshPaintableSurface(mf);
            return null;
        }

        /// <summary>Re-cook the MeshCollider (if any) against the deformed mesh.</summary>
        public static void RefreshCollider(MeshFilter filter)
        {
            if (filter == null) return;
            MeshDeformation marker = filter.GetComponent<MeshDeformation>();
            MeshCollider collider = filter.GetComponent<MeshCollider>();
            if (marker != null && marker.deformedMesh != null && collider != null)
                collider.sharedMesh = marker.deformedMesh;
        }

        public bool ApplyStamp(SculptStampArgs args)
        {
            Mesh mesh = EnsureDeformableMesh(out MeshDeformation marker);
            if (mesh == null) return false;

            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] weld = EnsureWeldClusters(marker, verts);
            Matrix4x4 l2w = filter.transform.localToWorldMatrix;
            Matrix4x4 w2l = filter.transform.worldToLocalMatrix;

            // Brush is a cylinder around the current hit normal (like terrain
            // brushes measure in map space): weight by lateral distance, so
            // already-displaced vertices keep full weight.
            Vector3 n = args.brushNormal.sqrMagnitude > 0.001f ? args.brushNormal.normalized
                : args.flattenNormal.sqrMagnitude > 0.001f ? args.flattenNormal.normalized : Vector3.up;
            Vector3 tangent = Vector3.Cross(n, Vector3.up);
            if (tangent.sqrMagnitude < 1e-6f) tangent = Vector3.right;
            tangent.Normalize();
            tangent = Quaternion.AngleAxis(args.rotation, n) * tangent;
            Vector3 bitangent = Vector3.Cross(n, tangent);

            float sqrRadius = args.radius * args.radius;
            var indices = new List<int>();
            var laterals = new List<float>();
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 rel = l2w.MultiplyPoint3x4(verts[i]) - args.center;
                float axial = Vector3.Dot(rel, n);
                if (Mathf.Abs(axial) > args.radius) continue;
                float lateralSqr = (rel - n * axial).sqrMagnitude;
                if (lateralSqr > sqrRadius) continue;
                indices.Add(i);
                laterals.Add(Mathf.Sqrt(lateralSqr));
            }
            if (indices.Count == 0) return false;

            var before = new Vector3[indices.Count];
            var after = new Vector3[indices.Count];
            for (int k = 0; k < indices.Count; k++) before[k] = verts[indices[k]];

            // Weld-averaged normals: co-located duplicates (UV seams, hard
            // edges) must displace identically or the surface tears open.
            Dictionary<int, Vector3> clusterNormals = null;
            if (args.op == SculptOp.Raise || args.op == SculptOp.Lower)
            {
                clusterNormals = new Dictionary<int, Vector3>();
                for (int k = 0; k < indices.Count; k++)
                {
                    int cluster = weld[indices[k]];
                    Vector3 wNormal = l2w.MultiplyVector(normals[indices[k]]);
                    clusterNormals[cluster] = clusterNormals.TryGetValue(cluster, out Vector3 acc) ? acc + wNormal : wNormal;
                }
            }

            // smooth relaxes toward the weighted local plane (adjacency-free)
            Vector3 centroid = Vector3.zero, avgNormal = Vector3.zero;
            if (args.op == SculptOp.Smooth)
            {
                for (int k = 0; k < indices.Count; k++)
                {
                    centroid += l2w.MultiplyPoint3x4(verts[indices[k]]);
                    avgNormal += l2w.MultiplyVector(normals[indices[k]]);
                }
                centroid /= indices.Count;
                avgNormal = avgNormal.sqrMagnitude > 1e-8f ? avgNormal.normalized : n;
            }

            float strength = Mathf.Clamp01(args.strength);
            for (int k = 0; k < indices.Count; k++)
            {
                int i = indices[k];
                Vector3 wPos = l2w.MultiplyPoint3x4(verts[i]);
                float d01 = laterals[k] / args.radius;
                float weight = Falloff(d01, args.hardness) * strength;
                if (weight <= 0f && args.op != SculptOp.Stamp)
                {
                    after[k] = verts[i];
                    continue;
                }

                switch (args.op)
                {
                    case SculptOp.Raise:
                    case SculptOp.Lower:
                    {
                        Vector3 wNormal = clusterNormals[weld[i]].normalized;
                        float step = args.radius * RaiseStepFraction * (args.op == SculptOp.Lower ? -1f : 1f);
                        wPos += wNormal * (step * weight);
                        break;
                    }
                    case SculptOp.Smooth:
                    {
                        float dist = Vector3.Dot(wPos - centroid, avgNormal);
                        wPos -= avgNormal * (dist * 0.5f * weight);
                        break;
                    }
                    case SculptOp.Flatten:
                    {
                        float dist = Vector3.Dot(wPos - args.flattenPoint, n);
                        wPos -= n * (dist * weight);
                        break;
                    }
                    case SculptOp.Stamp:
                    {
                        Vector3 rel = wPos - args.center;
                        float u = Vector3.Dot(rel, tangent) / args.radius;
                        float v = Vector3.Dot(rel, bitangent) / args.radius;
                        wPos += n * (SampleStamp(args, u, v) * args.stampHeight * strength);
                        break;
                    }
                }
                verts[i] = w2l.MultiplyPoint3x4(wPos);
                after[k] = verts[i];
            }

            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            recordHook?.Invoke(mesh, indices.ToArray(), before, after);
            return true;
        }

        public static float Falloff(float d01, float hardness)
        {
            if (d01 >= 1f) return 0f;
            if (hardness >= 0.999f) return 1f;
            float t = Mathf.Clamp01((d01 - hardness) / (1f - hardness));
            return 1f - t * t * (3f - 2f * t); // smoothstep
        }

        private static float SampleStamp(SculptStampArgs args, float u, float v)
        {
            if (args.stampTexture != null && args.stampTexture.isReadable)
                return args.stampTexture.GetPixelBilinear(u * 0.5f + 0.5f, v * 0.5f + 0.5f).r;
            // non-readable or missing texture: analytic round falloff
            return Falloff(Mathf.Sqrt(u * u + v * v), args.hardness);
        }

        private static int[] EnsureWeldClusters(MeshDeformation marker, Vector3[] verts)
        {
            if (marker.weldClusters != null && marker.weldClusters.Length == verts.Length)
                return marker.weldClusters;
            var firstAt = new Dictionary<Vector3, int>(verts.Length);
            var clusters = new int[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                if (firstAt.TryGetValue(verts[i], out int id)) clusters[i] = id;
                else { firstAt[verts[i]] = i; clusters[i] = i; }
            }
            marker.weldClusters = clusters;
            return clusters;
        }

        private Mesh EnsureDeformableMesh(out MeshDeformation marker)
        {
            marker = null;
            Mesh shared = filter.sharedMesh;
            if (shared == null) return null;

            marker = filter.GetComponent<MeshDeformation>();
            if (marker != null && marker.deformedMesh != null && shared == marker.deformedMesh)
                return shared;

            if (marker == null) marker = filter.gameObject.AddComponent<MeshDeformation>();
            Mesh clone = Object.Instantiate(shared);
            clone.name = shared.name + " (OmniBrush)";
            clone.MarkDynamic();
            marker.originalMesh = shared;
            marker.deformedMesh = clone;
            filter.sharedMesh = clone;
            return clone;
        }
    }
}
