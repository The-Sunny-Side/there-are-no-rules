using System.Linq;
using UnityEngine;

public enum VFXType { Drift, Boost, Landing }

public class VehicleVFX : MonoBehaviour
{
    [HideInInspector] public ParticleSystem ps;
    public VFXType type;
    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }
}

public struct VFXGroup
{
    public readonly VehicleVFX[] _effects;

    public VFXGroup(VehicleVFX[] effects) => _effects = effects;

    public static VFXGroup FromChildren(GameObject root, VFXType type)
    {
        Debug.Log($"VehicleVFX: Looking for {type} effects in {root.name}");
        var arr = root.GetComponentsInChildren<VehicleVFX>()
            .Where(v => v.type == type).ToArray();
        Debug.Log($"VehicleVFX: Found {arr.Length} {type} effects in {root.name}");
        return new VFXGroup(arr);
    }

    public void Play()
    {
        if (_effects == null) return;
        foreach (var vfx in _effects)
            if (!vfx.ps.isPlaying) vfx.ps.Play();
    }

    public void Stop()
    {
        if (_effects == null) return;
        foreach (var vfx in _effects)
            if (vfx.ps.isPlaying) vfx.ps.Stop();
    }
}