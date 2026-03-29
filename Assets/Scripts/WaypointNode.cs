using UnityEngine;

public class WaypointNode : MonoBehaviour
{
    public enum NodeType
    {
        Default,  // Rotation Minimizing Frame (smooth per curve)
        Flat,     // Vector3.up (piatto, ideale per bivi e incroci)
        Stop      // La spline continua ma la mesh non viene generata fino al nodo successivo
    }

    public NodeType nodeType = NodeType.Default;

    [Tooltip("Angolo di banking in gradi. Positivo = inclinazione verso l'interno della curva, Negativo = verso l'esterno")]
    [Range(-45f, 45f)]
    public float bankAngle = 0f;

    [Tooltip("Sovrascrive la tensione della spline a partire da questo nodo verso il successivo")]
    public bool overrideTension = false;

    [Tooltip("0 = curva morbida (Catmull-Rom), 1 = lineare")]
    [Range(0f, 1f)]
    public float splineTension = 0f;
}
