using System;
using UnityEngine;

namespace Lattice
{
	/// <summary>
	/// Useful lattice modifier extension methods.
	/// </summary>
	public static class ModifierExtensions
	{
		/// <summary>
		/// Reads back the deformed mesh from the GPU and stores it in a copy of the mesh.
		/// </summary>
		public static Mesh GetDeformedMesh(this LatticeModifierBase modifier)
		{
			return GetDeformedMesh(modifier, GetStaticVertexBuffer);
		}

		/// <summary>
		/// Reads back the deformed and skinned mesh from the GPU and stores it in a copy of the mesh. <br/>
		/// This will have save the current skinned posed as well.
		/// </summary>
		public static Mesh GetDeformedSkinnedMesh(this SkinnedLatticeModifier modifier)
		{
			return GetDeformedMesh(modifier, GetSkinnedVertexBuffer);
		}

		private static Mesh GetDeformedMesh<T>(T modifier, Func<T, GraphicsBuffer> getVertexBuffer) where T : LatticeModifierBase
		{
			// Create new mesh
			Mesh mesh = Mesh.Instantiate(modifier.Mesh);
			MeshInfo info = modifier.MeshInfo;

			// Get vertex buffer
			GraphicsBuffer vertexBuffer = getVertexBuffer(modifier);
			if (vertexBuffer == null)
			{
				Debug.LogError("Could not get the vertex buffer.");
				return null;
			}

			// Read vertex buffer
			int size = info.VertexCount * info.BufferStride;
			byte[] data = new byte[size];
			vertexBuffer.GetData(data);
			mesh.SetVertexBufferData(data, 0, 0, size, 0);

			if (info.HasAdditionalBuffer())
			{
				// Get additional vertex buffer
				GraphicsBuffer additionalBuffer = modifier.AdditionalBuffer;
				if (additionalBuffer == null)
				{
					Debug.LogError("Could not get the additional vertex buffer.");
					return null;
				}

				// Read additional vertex buffer
				int additionalSize = info.VertexCount * info.AdditionalStride;
				byte[] additionalData = new byte[additionalSize];
				additionalBuffer.GetData(additionalData);
				mesh.SetVertexBufferData(additionalData, 0, 0, additionalSize, 1);
			}

			return mesh;
		}

		private static GraphicsBuffer GetStaticVertexBuffer(LatticeModifierBase modifier)
		{
			return modifier.VertexBuffer;
		}

		private static GraphicsBuffer GetSkinnedVertexBuffer(SkinnedLatticeModifier modifier)
		{
			modifier.TryGetSkinnedBuffer(out var buffer);
			return buffer;
		}
	}
}
