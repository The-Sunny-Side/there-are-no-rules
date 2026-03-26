using UnityEngine;

public class WaypointNode : MonoBehaviour
{
    public enum NodeType
    {
        Default,  // Rotation Minimizing Frame (smooth per curve)
        Flat      // Vector3.up (piatto, ideale per bivi e incroci)
    }

    public NodeType nodeType = NodeType.Default;

    [Tooltip("Angolo di banking in gradi. Positivo = inclinazione verso l'interno della curva, Negativo = verso l'esterno")]
    [Range(-45f, 45f)]
    public float bankAngle = 0f;
}
