using UnityEngine;

/// <summary>
/// Gestisce la trail di boost: aggiorna le dimensioni sul materiale condiviso
/// e notifica il player al trigger enter.
/// </summary>
[ExecuteAlways]
public class SpeedTrail : MonoBehaviour
{
    [Tooltip("Forza del boost applicata in avanti (unità/s aggiuntive a forwardSpeed).")]
    public float boostForce = 20f;

    public float arrowSize = 1f;

    private static readonly int P_QuadWidth  = Shader.PropertyToID("_QuadWidth");
    private static readonly int P_QuadLength = Shader.PropertyToID("_QuadLength");
    private static readonly int P_ArrowSize = Shader.PropertyToID("_ArrowSize");

    private MeshRenderer _renderer;

    private void OnEnable()
    {
        _renderer = GetComponentInChildren<MeshRenderer>();
        Push();
    }

    private void Update() => Push();

    private void Push()
    {
        if (_renderer == null || _renderer.sharedMaterial == null) return;
        Vector3 s = transform.localScale;
        _renderer.sharedMaterial.SetFloat(P_QuadWidth,  Mathf.Abs(s.x));
        _renderer.sharedMaterial.SetFloat(P_QuadLength, Mathf.Abs(s.z));
        _renderer.sharedMaterial.SetFloat(P_ArrowSize, Mathf.Abs(arrowSize));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerSphere")) return;
        other.GetComponent<NewMovementPredicted>()?.OnSpeedTrailEnter(this);
    }

    [ContextMenu("Snap To Ground")]
    private void SnapToGround()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 20f))
        {
            transform.position = hit.point;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal)
                               * Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }
    }
}
