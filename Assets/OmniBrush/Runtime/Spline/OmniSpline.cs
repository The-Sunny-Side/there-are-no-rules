using System.Collections.Generic;
using UnityEngine;

namespace OmniBrush
{
    /// <summary>Catmull-Rom spline for road/river style operations along a path.</summary>
    public class OmniSpline : MonoBehaviour
    {
        [Tooltip("Control points in local space. Select the object to edit them with scene handles; Shift+Click in the scene appends a point.")]
        public List<Vector3> localPoints = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(12f, 0f, 4f),
            new Vector3(24f, 0f, 0f),
        };

        public int PointCount => localPoints.Count;

        public Vector3 GetWorldPoint(int index) => transform.TransformPoint(localPoints[index]);

        public void SetWorldPoint(int index, Vector3 world) =>
            localPoints[index] = transform.InverseTransformPoint(world);

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        /// <summary>World position on segment i (between point i and i+1) at u in [0,1].</summary>
        public Vector3 EvaluateSegment(int i, float u)
        {
            int n = localPoints.Count;
            Vector3 p0 = localPoints[Mathf.Max(i - 1, 0)];
            Vector3 p1 = localPoints[i];
            Vector3 p2 = localPoints[Mathf.Min(i + 1, n - 1)];
            Vector3 p3 = localPoints[Mathf.Min(i + 2, n - 1)];
            return transform.TransformPoint(CatmullRom(p0, p1, p2, p3, u));
        }

        /// <summary>Approximately equidistant world samples along the whole spline.</summary>
        public List<Vector3> SampleByDistance(float spacing)
        {
            var result = new List<Vector3>();
            if (localPoints.Count < 2) return result;
            spacing = Mathf.Max(0.05f, spacing);

            const int Subdiv = 16;
            var dense = new List<Vector3>((localPoints.Count - 1) * Subdiv + 1);
            for (int i = 0; i < localPoints.Count - 1; i++)
                for (int k = 0; k < Subdiv; k++)
                    dense.Add(EvaluateSegment(i, k / (float)Subdiv));
            dense.Add(GetWorldPoint(localPoints.Count - 1));

            result.Add(dense[0]);
            float carry = 0f;
            for (int i = 1; i < dense.Count; i++)
            {
                Vector3 prev = dense[i - 1];
                Vector3 current = dense[i];
                float segment = Vector3.Distance(prev, current);
                while (carry + segment >= spacing && segment > 1e-6f)
                {
                    float need = spacing - carry;
                    prev += (current - prev).normalized * need;
                    result.Add(prev);
                    segment = Vector3.Distance(prev, current);
                    carry = 0f;
                }
                carry += segment;
            }
            return result;
        }

        // Always visible in the Scene view (gizmos are editor-only), selected or not.
        private void OnDrawGizmos()
        {
            if (localPoints.Count < 2) return;
            Gizmos.color = new Color(1f, 0.55f, 0f, 0.9f);
            List<Vector3> samples = SampleByDistance(1f);
            for (int i = 1; i < samples.Count; i++)
                Gizmos.DrawLine(samples[i - 1], samples[i]);
            Gizmos.color = new Color(1f, 0.85f, 0.2f);
            for (int i = 0; i < localPoints.Count; i++)
                Gizmos.DrawSphere(GetWorldPoint(i), 0.25f);
        }
    }
}
