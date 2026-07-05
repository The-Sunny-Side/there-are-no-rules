using UnityEngine;

public enum GroundSurfaceType { Snow, Rock }

public class SurfaceTypeTag : MonoBehaviour
{
    public GroundSurfaceType surfaceType = GroundSurfaceType.Rock;
}
