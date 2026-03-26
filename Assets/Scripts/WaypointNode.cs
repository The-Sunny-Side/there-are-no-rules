using UnityEngine;

public class WaypointNode : MonoBehaviour
{
    public enum NodeType
    {
        Default,  // Rotation Minimizing Frame (smooth per curve)
        Flat      // Vector3.up (piatto, ideale per bivi e incroci)
    }

    public NodeType nodeType = NodeType.Default;
}
