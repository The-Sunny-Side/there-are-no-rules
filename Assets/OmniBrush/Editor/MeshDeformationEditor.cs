using UnityEditor;
using UnityEngine;

namespace OmniBrush.Editor
{
    [CustomEditor(typeof(MeshDeformation))]
    public class MeshDeformationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var deformation = (MeshDeformation)target;
            EditorGUILayout.LabelField("Original Mesh",
                deformation.originalMesh != null ? deformation.originalMesh.name : "-");
            EditorGUILayout.HelpBox("This mesh was cloned by OmniBrush — the shared asset is untouched.", MessageType.None);

            if (GUILayout.Button("Revert To Original Mesh") && deformation.originalMesh != null)
            {
                MeshFilter filter = deformation.GetComponent<MeshFilter>();
                if (filter != null)
                {
                    Undo.RecordObject(filter, "Revert OmniBrush Mesh");
                    filter.sharedMesh = deformation.originalMesh;
                }
                MeshCollider collider = deformation.GetComponent<MeshCollider>();
                if (collider != null && deformation.deformedMesh != null && collider.sharedMesh == deformation.deformedMesh)
                {
                    Undo.RecordObject(collider, "Revert OmniBrush Mesh");
                    collider.sharedMesh = deformation.originalMesh;
                }
                Undo.DestroyObjectImmediate(deformation);
            }
        }
    }
}
