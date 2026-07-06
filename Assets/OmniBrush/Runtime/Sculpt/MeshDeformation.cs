using UnityEngine;

namespace OmniBrush
{
    /// <summary>
    /// Marks a MeshFilter whose mesh was cloned for sculpting. The shared
    /// asset is never modified; the clone lives in the scene. Holds the
    /// original for revert (inspector button).
    /// </summary>
    [DisallowMultipleComponent]
    public class MeshDeformation : MonoBehaviour
    {
        public Mesh originalMesh;
        public Mesh deformedMesh;
    }
}
