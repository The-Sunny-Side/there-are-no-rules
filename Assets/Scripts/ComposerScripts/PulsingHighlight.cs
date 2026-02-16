using System.Collections.Generic;
using UnityEngine;

public class PulsingHighlight : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private GameObject[] targets;

    [Header("Pulse Settings")]
    [SerializeField] private float intensityMin = 0f;
    [SerializeField] private float intensityMax = 1f;
    [SerializeField] private float speed = 2f;

    private class PulseData
    {
        public Renderer renderer;
        public MaterialPropertyBlock mpb;
        public Color baseColor;
        public int colorPropertyID;
    }

    private List<PulseData> pulseRenderers = new List<PulseData>();

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

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) continue;

            var mat = renderer.sharedMaterial;
            if (mat == null) continue;

            int propertyID;
            Color baseColor;

            if (mat.HasProperty(BaseColorID))
            {
                propertyID = BaseColorID;
                baseColor = mat.GetColor(BaseColorID);
            }
            else if (mat.HasProperty(ColorID))
            {
                propertyID = ColorID;
                baseColor = mat.GetColor(ColorID);
            }
            else
            {
                continue; // materiale non supportato
            }

            pulseRenderers.Add(new PulseData
            {
                renderer = renderer,
                mpb = new MaterialPropertyBlock(),
                baseColor = baseColor,
                colorPropertyID = propertyID
            });
        }
    }

    void Update()
    {
        if (pulseRenderers.Count == 0) return;

        float t = Mathf.PingPong(Time.time * speed, 1f);
        float intensity = Mathf.Lerp(intensityMin, intensityMax, t);

        foreach (var data in pulseRenderers)
        {
            // Usa direttamente baseColor modulato per intensità
            Color finalColor = data.baseColor * intensity;

            data.renderer.GetPropertyBlock(data.mpb);
            data.mpb.SetColor(data.colorPropertyID, finalColor);
            data.renderer.SetPropertyBlock(data.mpb);
        }
    }

    void OnDisable()
    {
        foreach (var data in pulseRenderers)
        {
            data.renderer.GetPropertyBlock(data.mpb);
            data.mpb.SetColor(data.colorPropertyID, data.baseColor);
            data.renderer.SetPropertyBlock(data.mpb);
        }
    }
}
