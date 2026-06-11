using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class FlagMeshGenerator : MonoBehaviour
{
    [Header("Mesh")]
    public float width = 2f;
    public float height = 1f;
    public int columns = 20;
    public int rows = 10;

    [Header("Sfondo")]
    public bool useBackgroundTexture = false;
    public Color backgroundColor = Color.red;
    public Texture2D backgroundTexture;

    [Header("Contenuto")]
    public Texture2D contentTexture;

    [Header("Onda")]
    public float waveAmplitude = 0.08f;
    public float waveFrequency = 3.0f;
    public float waveSpeed = 2.0f;

    // ----------------------------------------------------------------
    static readonly int PropUseBackgroundTex = Shader.PropertyToID("_UseBackgroundTex");
    static readonly int PropBackgroundColor = Shader.PropertyToID("_BackgroundColor");
    static readonly int PropBackgroundTex = Shader.PropertyToID("_BackgroundTex");
    static readonly int PropContentTex = Shader.PropertyToID("_ContentTex");
    static readonly int PropWaveAmplitude = Shader.PropertyToID("_WaveAmplitude");
    static readonly int PropWaveFrequency = Shader.PropertyToID("_WaveFrequency");
    static readonly int PropWaveSpeed = Shader.PropertyToID("_WaveSpeed");

    MeshRenderer _renderer;
    Material _material;

    // ----------------------------------------------------------------
    void OnEnable()
    {
        _renderer = GetComponent<MeshRenderer>();
        BuildMesh();
        SetupMaterial();
    }

    void OnValidate()
    {
        // Chiamato ogni volta che un campo cambia nell'Inspector
        BuildMesh();
        ApplyMaterialProperties();
    }

    // ----------------------------------------------------------------
    void BuildMesh()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null) return;

        var mesh = new Mesh { name = "FlagMesh" };
        int vertsX = columns + 1;
        int vertsY = rows + 1;

        var vertices = new Vector3[vertsX * vertsY];
        var uvs = new Vector2[vertsX * vertsY];
        var triangles = new int[columns * rows * 6];

        for (int y = 0; y < vertsY; y++)
            for (int x = 0; x < vertsX; x++)
            {
                int i = y * vertsX + x;
                float u = (float)x / columns;
                float v = (float)y / rows;
                vertices[i] = new Vector3(u * width, v * height, 0f);
                uvs[i] = new Vector2(u, v);
            }

        int t = 0;
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int bl = y * vertsX + x;
                int br = bl + 1;
                int tl = bl + vertsX;
                int tr = tl + 1;
                triangles[t++] = bl; triangles[t++] = tl; triangles[t++] = tr;
                triangles[t++] = bl; triangles[t++] = tr; triangles[t++] = br;
            }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        mf.mesh = mesh;
    }

    // ----------------------------------------------------------------
    void SetupMaterial()
    {
        if (_renderer == null) return;

        // Crea una istanza del materiale dedicata a questo GameObject
        // per evitare di modificare l'asset condiviso
        var sharedMat = _renderer.sharedMaterial;
        if (sharedMat == null)
        {
            Debug.LogWarning("[FlagMeshGenerator] Nessun materiale assegnato al MeshRenderer.");
            return;
        }

        _material = Application.isPlaying
            ? _renderer.material           // istanza automatica in play mode
            : new Material(sharedMat);     // istanza manuale in edit mode

        if (!Application.isPlaying)
            _renderer.sharedMaterial = _material;

        ApplyMaterialProperties();
    }

    void ApplyMaterialProperties()
    {
        if (_material == null)
        {
            SetupMaterial();
            return;
        }

        _material.SetFloat(PropUseBackgroundTex, useBackgroundTexture ? 1f : 0f);
        _material.SetColor(PropBackgroundColor, backgroundColor);
        _material.SetFloat(PropWaveAmplitude, waveAmplitude);
        _material.SetFloat(PropWaveFrequency, waveFrequency);
        _material.SetFloat(PropWaveSpeed, waveSpeed);

        if (useBackgroundTexture && backgroundTexture != null)
            _material.SetTexture(PropBackgroundTex, backgroundTexture);

        if (contentTexture != null)
            _material.SetTexture(PropContentTex, contentTexture);

        // Keyword per lo shader_feature
        if (useBackgroundTexture)
            _material.EnableKeyword("_USEBACKGROUNDTEX_ON");
        else
            _material.DisableKeyword("_USEBACKGROUNDTEX_ON");
    }
}