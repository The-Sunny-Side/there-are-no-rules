using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class PulsingHighlight : MonoBehaviour
{
    [Header("Pulse Settings")]
    [SerializeField] private float intensityMin = 0.05f;
    [SerializeField] private float intensityMax = 0.4f;
    [SerializeField] private float speed = 2f;

    private Material mat;
    private Color baseColor;
    private static readonly int EmissionID = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor"); // URP
    private static readonly int ColorID = Shader.PropertyToID("_Color");         // Built-in

    void Awake()
    {
        mat = GetComponent<MeshRenderer>().material;
        mat.EnableKeyword("_EMISSION");

        // URP / HDRP usa _BaseColor, Built-in usa _Color
        if (mat.HasProperty(BaseColorID))
            baseColor = mat.GetColor(BaseColorID);
        else
            baseColor = mat.GetColor(ColorID);
    }

    void Update()
    {
        float t = Mathf.PingPong(Time.time * speed, 1f);
        float intensity = Mathf.Lerp(intensityMin, intensityMax, t);

        mat.SetColor(EmissionID, baseColor * intensity);
    }

    void OnDisable()
    {
        // Reset emission quando disattivato
        mat.SetColor(EmissionID, Color.black);
    }
}
