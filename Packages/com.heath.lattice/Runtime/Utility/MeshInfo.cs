using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice
{
	/// <summary>
	/// Information about the vertex buffer of a mesh, 
	/// including count, stride and offsets.
	/// </summary>
	public readonly struct MeshInfo
	{
		public readonly int VertexCount;
		public readonly int BufferStride;
		public readonly int AdditionalStride;

		// Main buffer
		public readonly int PositionOffset;
		public readonly int NormalOffset;
		public readonly int TangentOffset;

		// Additional buffer
		public readonly int ColorOffset;
		public readonly int TexCoord0Offset;
		public readonly int TexCoord1Offset;
		public readonly int TexCoord2Offset;
		public readonly int TexCoord3Offset;
		public readonly int TexCoord4Offset;
		public readonly int TexCoord5Offset;
		public readonly int TexCoord6Offset;
		public readonly int TexCoord7Offset;

		public MeshInfo(Mesh mesh)
		{
			VertexCount = mesh.vertexCount;
			BufferStride = mesh.GetVertexBufferStride(0);
			AdditionalStride = mesh.GetVertexBufferStride(1);

			PositionOffset = mesh.GetVertexAttributeOffset(VertexAttribute.Position);
			NormalOffset = mesh.GetVertexAttributeOffset(VertexAttribute.Normal);
			TangentOffset = mesh.GetVertexAttributeOffset(VertexAttribute.Tangent);

			ColorOffset = mesh.GetVertexAttributeOffset(VertexAttribute.Color);
			TexCoord0Offset = mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord0);
			TexCoord1Offset = mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord1);
			TexCoord2Offset = mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord2);
			TexCoord3Offset = mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord3);
			TexCoord4Offset = mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord4);
			TexCoord5Offset = mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord5);
			TexCoord6Offset = mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord6);
			TexCoord7Offset = mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord7);
		}

		public readonly int GetTexCoordOffset(int index)
		{
			return index switch
			{
				0 => TexCoord0Offset,
				1 => TexCoord1Offset,
				2 => TexCoord2Offset,
				3 => TexCoord3Offset,
				4 => TexCoord4Offset,
				5 => TexCoord5Offset,
				6 => TexCoord6Offset,
				7 => TexCoord7Offset,
				_ => -1,
			};
		}

		public readonly bool HasAdditionalBuffer() => AdditionalStride != 0;
	}
}
