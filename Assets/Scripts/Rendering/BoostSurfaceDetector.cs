using UnityEngine;

// Recolors the boost kick-up spray based on what's under the vehicle (snow vs. generic/rock).
// Standalone: does not touch NewMovementPredicted's own ground-align raycasts.
public class BoostSurfaceDetector : MonoBehaviour
{
    public ParticleSystem kickup;
    public float rayDistance = 3f;
    public float rayHeight = 0.5f;

    static readonly Color[] SnowColors = { new Color(0.85f, 0.95f, 1f), Color.white };
    static readonly Color[] RockColors = { new Color(0.55f, 0.42f, 0.3f), new Color(0.75f, 0.62f, 0.45f) };

    int _groundMask;
    GroundSurfaceType _current;
    bool _hasCurrent;

    void Awake() => _groundMask = LayerMask.GetMask("Ground");

    void LateUpdate()
    {
        if (kickup == null || !kickup.isPlaying) return;

        var surface = GroundSurfaceType.Snow;
        var origin = transform.position + Vector3.up * rayHeight;
        if (Physics.Raycast(origin, Vector3.down, out var hit, rayDistance, _groundMask, QueryTriggerInteraction.Ignore))
        {
            var tag = hit.collider.GetComponentInParent<SurfaceTypeTag>();
            if (tag != null) surface = tag.surfaceType;
        }

        if (_hasCurrent && surface == _current) return;
        _current = surface;
        _hasCurrent = true;

        var main = kickup.main;
        var palette = surface == GroundSurfaceType.Snow ? SnowColors : RockColors;
        main.startColor = new ParticleSystem.MinMaxGradient(palette[0], palette[1]);
    }
}
