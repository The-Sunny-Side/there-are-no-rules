using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class OutlineRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Color outlineColor = Color.black;
        [Range(0.5f, 8f)]      public float thickness       = 1.5f;
        [Range(0.0001f, 0.1f)] public float depthThreshold  = 0.005f;
        [Range(0f, 2f)]        public float normalThreshold = 0.4f;
    }

    public Settings settings = new Settings();

    [SerializeField, HideInInspector]
    private Shader _outlineShader;

    private OutlinePass _pass;
    private Material    _material;

    public override void Create()
    {
        if (_outlineShader == null)
            _outlineShader = Shader.Find("Hidden/Outline");
        if (_outlineShader == null)
        {
            Debug.LogWarning("[OutlineRendererFeature] Shader 'Hidden/Outline' not found.");
            return;
        }
        _material = CoreUtils.CreateEngineMaterial(_outlineShader);
        _pass     = new OutlinePass(name);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null || _pass == null) return;
        if (renderingData.cameraData.cameraType == CameraType.Preview) return;

        _pass.Setup(_material, settings);
        _pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_material);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

public class OutlinePass : ScriptableRenderPass
{
    private class CopyData { public TextureHandle source; }
    private class DrawData { public TextureHandle source; public Material material; }

    private Material _material;
    private OutlineRendererFeature.Settings _settings;
    private readonly string _tag;

    private static readonly int s_OutlineColor    = Shader.PropertyToID("_OutlineColor");
    private static readonly int s_Thickness       = Shader.PropertyToID("_Thickness");
    private static readonly int s_DepthThreshold  = Shader.PropertyToID("_DepthThreshold");
    private static readonly int s_NormalThreshold = Shader.PropertyToID("_NormalThreshold");

    public OutlinePass(string tag)
    {
        _tag = tag;
        requiresIntermediateTexture = true;
        ConfigureInput(ScriptableRenderPassInput.Color |
                       ScriptableRenderPassInput.Depth |
                       ScriptableRenderPassInput.Normal);
    }

    public void Setup(Material material, OutlineRendererFeature.Settings settings)
    {
        _material = material;
        _settings = settings;

        // Set on main thread — shader reads these as globals
        Shader.SetGlobalColor(s_OutlineColor,    settings.outlineColor);
        Shader.SetGlobalFloat(s_Thickness,       settings.thickness);
        Shader.SetGlobalFloat(s_DepthThreshold,  settings.depthThreshold);
        Shader.SetGlobalFloat(s_NormalThreshold, settings.normalThreshold);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        if (resourceData.isActiveTargetBackBuffer) return;

        var cameraData = frameData.Get<UniversalCameraData>();
        var desc = cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples     = 1;
        TextureHandle tempColor = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, desc, "_OutlineSourceCopy", false);

        // Pass 1: copy activeColor → tempColor
        using (var builder = renderGraph.AddRasterRenderPass<CopyData>(_tag + "_Copy", out var pd))
        {
            pd.source = resourceData.activeColorTexture;
            builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
            builder.SetRenderAttachment(tempColor, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (CopyData d, RasterGraphContext ctx) =>
            {
                Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1f, 1f, 0f, 0f), 0, false);
            });
        }

        // Pass 2: outline blit tempColor → activeColorTexture
        using (var builder = renderGraph.AddRasterRenderPass<DrawData>(_tag, out var pd))
        {
            pd.source   = tempColor;
            pd.material = _material;
            builder.UseTexture(tempColor, AccessFlags.Read);
            builder.UseAllGlobalTextures(true);
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (DrawData d, RasterGraphContext ctx) =>
            {
                Blitter.BlitTexture(ctx.cmd, d.source,
                    new Vector4(1f, 1f, 0f, 0f), d.material, 0);
            });
        }
    }
}
