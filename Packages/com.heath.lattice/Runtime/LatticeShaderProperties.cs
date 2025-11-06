using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice
{
	/// <summary>
	/// All lattice shader keywords and IDs.
	/// </summary>
	public class LatticeShaderProperties
	{
		public static readonly int VertexBufferId = Shader.PropertyToID("VertexBuffer");
		public static readonly int VertexCountId  = Shader.PropertyToID("VertexCount");
		public static readonly int BufferStrideId = Shader.PropertyToID("BufferStride");

		public static readonly int PositionOffsetId = Shader.PropertyToID("PositionOffset");
		public static readonly int NormalOffsetId   = Shader.PropertyToID("NormalOffset");
		public static readonly int TangentOffsetId  = Shader.PropertyToID("TangentOffset");

		public static readonly int AdditionalBufferId = Shader.PropertyToID("AdditionalBuffer");
		public static readonly int AdditionalStrideId = Shader.PropertyToID("AdditionalStride");
		public static readonly int StretchOffsetId    = Shader.PropertyToID("StretchOffset");

		public static readonly int LatticeBufferId     = Shader.PropertyToID("LatticeBuffer");
		public static readonly int ObjectToLatticeId   = Shader.PropertyToID("ObjectToLattice");
		public static readonly int LatticeToObjectId   = Shader.PropertyToID("LatticeToObject");
		public static readonly int LatticeResolutionId = Shader.PropertyToID("LatticeResolution");

		public static readonly int MaskOffsetId     = Shader.PropertyToID("MaskOffset");
		public static readonly int MaskChannelId    = Shader.PropertyToID("MaskChannel");
		public static readonly int MaskTextureId    = Shader.PropertyToID("MaskTexture");
		public static readonly int MaskMultiplierId = Shader.PropertyToID("MaskMultiplier");

		public static readonly int IndexToVertexMapId = Shader.PropertyToID("IndexToVertexMap");
		public static readonly int IndexCountId       = Shader.PropertyToID("IndexCount");

		public readonly LocalKeyword NormalsKeyword;
		public readonly LocalKeyword StretchKeyword;
		public readonly LocalKeyword MultipleBuffersKeyword;

		public readonly LocalKeyword InterpolationSmooth;
		public readonly LocalKeyword InterpolationCubic;
		public readonly LocalKeyword ZeroOutsideKeyword;
		public readonly LocalKeyword MaskColorKeyword;
		public readonly LocalKeyword MaskUVKeyword;
		public readonly LocalKeyword MaskTextureKeyword;
		public readonly LocalKeyword MaskConstantKeyword;
		public readonly LocalKeyword UseIndicesKeyword;

		private readonly ComputeShader _shader;

		public LatticeShaderProperties(ComputeShader shader)
		{
			NormalsKeyword         = new LocalKeyword(shader, "LATTICE_NORMALS");
			StretchKeyword         = new LocalKeyword(shader, "LATTICE_STRETCH");
			MultipleBuffersKeyword = new LocalKeyword(shader, "LATTICE_MULTIPLE_BUFFERS");
			InterpolationSmooth    = new LocalKeyword(shader, "LATTICE_INTRP_SMOOTH");
			InterpolationCubic	   = new LocalKeyword(shader, "LATTICE_INTRP_CUBIC");
			ZeroOutsideKeyword     = new LocalKeyword(shader, "LATTICE_ZERO_OUTSIDE");
			MaskColorKeyword       = new LocalKeyword(shader, "LATTICE_MASK_COLOR");
			MaskUVKeyword          = new LocalKeyword(shader, "LATTICE_MASK_UV");
			MaskTextureKeyword     = new LocalKeyword(shader, "LATTICE_MASK_TEXTURE");
			MaskConstantKeyword    = new LocalKeyword(shader, "LATTICE_MASK_CONSTANT");
			UseIndicesKeyword      = new LocalKeyword(shader, "LATTICE_USE_INDICES");

			_shader = shader;
		}

		public void DisableAllKeywords()
		{
			_shader.DisableKeyword(NormalsKeyword);
			_shader.DisableKeyword(StretchKeyword);
			_shader.DisableKeyword(MultipleBuffersKeyword);
			_shader.DisableKeyword(InterpolationSmooth);
			_shader.DisableKeyword(InterpolationCubic);
			_shader.DisableKeyword(ZeroOutsideKeyword);
			_shader.DisableKeyword(MaskColorKeyword);
			_shader.DisableKeyword(MaskUVKeyword);
			_shader.DisableKeyword(MaskTextureKeyword);
			_shader.DisableKeyword(MaskConstantKeyword);
			_shader.DisableKeyword(UseIndicesKeyword);
		}
	}
}