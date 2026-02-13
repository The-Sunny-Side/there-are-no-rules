using System.Collections.Generic;
using UnityEngine;

public class PulsingHighlight : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private GameObject[] targets;

    [Header("Pulse Settings")]
    [SerializeField] private float intensityMin = 0.05f;
    [SerializeField] private float intensityMax = 0.4f;
    [SerializeField] private float speed = 2f;

    private class PulseData
    {
        public Material mat;
        public Color baseColor;
    }

    private List<PulseData> pulseMaterials = new List<PulseData>();

    private static readonly int EmissionID = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    void Awake()
    {
        GameObject[] effectiveTargets = (targets == null || targets.Length == 0)
            ? new GameObject[] { gameObject }
            : targets;

        foreach (var go in effectiveTargets)
        {
            if (go == null) continue;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null) continue;

            Material mat = renderer.material;
            mat.EnableKeyword("_EMISSION");

            Color baseColor = mat.HasProperty(BaseColorID)
                ? mat.GetColor(BaseColorID)
                : mat.GetColor(ColorID);

            pulseMaterials.Add(new PulseData
            {
                mat = mat,
                baseColor = baseColor
            });
        }
    }

    void Update()
    {
        if (pulseMaterials.Count == 0) return;

        float t = Mathf.PingPong(Time.time * speed, 1f);
        float intensity = Mathf.Lerp(intensityMin, intensityMax, t);

        foreach (var data in pulseMaterials)
        {
            data.mat.SetColor(EmissionID, data.baseColor * intensity);
        }
    }

    void OnDisable()
    {
        foreach (var data in pulseMaterials)
        {
            if (data.mat != null)
                data.mat.SetColor(EmissionID, Color.black);
        }
    }
}
