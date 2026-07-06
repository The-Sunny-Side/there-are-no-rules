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

        /// <summary>
        /// Per-vertex weld cluster ids (index of the first co-located vertex).
        /// Duplicated seam/hard-edge vertices must move together or the mesh
        /// tears apart. Built lazily by MeshPaintableSurface.
        /// </summary>
        [System.NonSerialized] public int[] weldClusters;
    }
}
