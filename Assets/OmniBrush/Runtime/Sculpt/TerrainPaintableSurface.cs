using UnityEngine;
using UnityEngine.TerrainTools;

namespace OmniBrush
{
    /// <summary>
    /// Terrain implementation of IPaintableSurface. All ops run on the GPU via
    /// PaintContext, which also stitches strokes across neighbor terrain tiles.
    /// </summary>
    public class TerrainPaintableSurface : IPaintableSurface
    {
        public delegate void PaintContextCapture(PaintContext context, bool before);

        /// <summary>Editor hook: invoked before/after each stamp for undo capture.</summary>
        public static PaintContextCapture captureHook;

        private readonly Terrain terrain;

        public TerrainPaintableSurface(Terrain terrain) => this.terrain = terrain;

        public Object Target => terrain;

        public static TerrainPaintableSurface TryFrom(Collider collider)
        {
            if (collider is TerrainCollider)
            {
                Terrain t = collider.GetComponent<Terrain>();
                if (t != null && t.terrainData != null) return new TerrainPaintableSurface(t);
            }
            return null;
        }

        public bool ApplyStamp(SculptStampArgs args)
        {
            TerrainData data = terrain.terrainData;
            Vector3 local = args.center - terrain.transform.position;
            var uv = new Vector2(local.x / data.size.x, local.z / data.size.z);

            BrushTransform xf = TerrainPaintUtility.CalculateBrushTransform(terrain, uv, args.radius * 2f, args.rotation);
            PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(terrain, xf.GetBrushXYBounds(), 0, true);
            if (ctx == null) return false;

            captureHook?.Invoke(ctx, true);

            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();
            Texture brush = args.stampTexture != null ? (Texture)args.stampTexture : SculptBrushTexture.Get(args.hardness);
            mat.SetTexture("_BrushTex", brush);

            TerrainBuiltinPaintMaterialPasses pass;
            switch (args.op)
            {
                case SculptOp.Smooth:
                    pass = TerrainBuiltinPaintMaterialPasses.SmoothHeights;
                    mat.SetVector("_BrushParams", new Vector4(Mathf.Clamp01(args.strength), 0f, 0f, 0f));
                    mat.SetVector("_SmoothWeights", new Vector4(1f, 0f, 0f, 0f)); // centered blur
                    break;

                case SculptOp.Flatten:
                {
                    pass = TerrainBuiltinPaintMaterialPasses.SetHeights;
                    float target01 = Mathf.Clamp01((args.flattenHeight - terrain.transform.position.y) / data.size.y);
                    // 0.01 factor matches Unity's SetHeightTool; the shader's
                    // smoothing math divides by (1 - strength) and NaNs at 1.0.
                    float s = Mathf.Clamp01(args.strength) * 0.01f;
                    mat.SetVector("_BrushParams",
                        new Vector4(s, PaintContext.kNormalizedHeightScale * target01, 0f, 0f));
                    break;
                }

                case SculptOp.Stamp:
                {
                    // z = packed target height, x = strength, w = 0 max / 1 add
                    pass = TerrainBuiltinPaintMaterialPasses.StampHeight;
                    float h01 = Mathf.Clamp01(args.stampHeight / data.size.y);
                    mat.SetVector("_BrushParams", new Vector4(
                        Mathf.Clamp01(args.strength), 0f,
                        PaintContext.kNormalizedHeightScale * h01,
                        args.stampAdditive ? 1f : 0f));
                    break;
                }

                default: // Raise / Lower — same speed scaling as Unity's built-in tool
                {
                    pass = TerrainBuiltinPaintMaterialPasses.RaiseLowerHeight;
                    float s = Mathf.Clamp01(args.strength) * 0.01f * (args.op == SculptOp.Lower ? -1f : 1f);
                    mat.SetVector("_BrushParams", new Vector4(s, 0f, 0f, 0f));
                    break;
                }
            }

            TerrainPaintUtility.SetupTerrainToolMaterialProperties(ctx, xf, mat);
            Graphics.Blit(ctx.sourceRenderTexture, ctx.destinationRenderTexture, mat, (int)pass);
            TerrainPaintUtility.EndPaintHeightmap(ctx, null);
            PaintContext.ApplyDelayedActions(); // sync GPU edits back to CPU heights now

            captureHook?.Invoke(ctx, false);
            return true;
        }

        /// <summary>Paints a TerrainLayer's weight (splat). The layer is added to the terrain if missing.</summary>
        public bool ApplyTexturePaint(Vector3 center, float radius, float strength, float hardness, TerrainLayer layer)
        {
            if (layer == null) return false;
            TerrainData data = terrain.terrainData;

            // auto-add the layer; undo restores weights but keeps the added layer (harmless)
            TerrainLayer[] layers = data.terrainLayers;
            bool found = false;
            for (int i = 0; i < layers.Length; i++)
                if (layers[i] == layer) { found = true; break; }
            if (!found)
            {
                System.Array.Resize(ref layers, layers.Length + 1);
                layers[layers.Length - 1] = layer;
                data.terrainLayers = layers;
            }

            Vector3 local = center - terrain.transform.position;
            var uv = new Vector2(local.x / data.size.x, local.z / data.size.z);
            BrushTransform xf = TerrainPaintUtility.CalculateBrushTransform(terrain, uv, radius * 2f, 0f);
            PaintContext ctx = TerrainPaintUtility.BeginPaintTexture(terrain, xf.GetBrushXYBounds(), layer);
            if (ctx == null) return false;

            captureHook?.Invoke(ctx, true);

            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();
            mat.SetTexture("_BrushTex", SculptBrushTexture.Get(hardness));
            mat.SetVector("_BrushParams", new Vector4(Mathf.Clamp01(strength), 1f, 0f, 0f)); // y = target alpha
            TerrainPaintUtility.SetupTerrainToolMaterialProperties(ctx, xf, mat);
            Graphics.Blit(ctx.sourceRenderTexture, ctx.destinationRenderTexture, mat,
                (int)TerrainBuiltinPaintMaterialPasses.PaintTexture);
            TerrainPaintUtility.EndPaintTexture(ctx, null);
            PaintContext.ApplyDelayedActions();

            captureHook?.Invoke(ctx, false);
            return true;
        }
    }
}
