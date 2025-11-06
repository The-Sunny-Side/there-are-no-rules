using UnityEngine;

namespace Lattice
{
	/// <summary>
	/// Lattice modifier for mesh filter and renderers.
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class LatticeModifier : LatticeModifierBase
	{
		#region Fields

		private MeshFilter _meshFilter;
		private MeshRenderer _meshRenderer;

		#endregion

		#region Properties

		/// <inheritdoc cref="LatticeModifierBase.Renderer"/>
		private MeshRenderer MeshRenderer => (_meshRenderer == null)
			? _meshRenderer = GetComponent<MeshRenderer>()
			: _meshRenderer;

		/// <summary>
		/// Retrieves the mesh filter on the current object.
		/// </summary>
		private MeshFilter MeshFilter => (_meshFilter == null)
			? _meshFilter = GetComponent<MeshFilter>()
			: _meshFilter;

		#endregion

		#region Protected Methods

		/// <inheritdoc cref="LatticeModifierBase.GetMesh"/>
		protected override Mesh GetMesh()
		{
			return MeshFilter.sharedMesh;
		}

		/// <inheritdoc cref="LatticeModifierBase.SetMesh"/>
		protected override void SetMesh(Mesh mesh)
		{
			MeshFilter.sharedMesh = mesh;
		}

		/// <inheritdoc cref="LatticeModifierBase.Enqueue"/>
		protected override void Enqueue(bool ignoreMode)
		{
			bool isVisible = MeshRenderer.isVisible;

#if UNITY_EDITOR
			// Update when in editor mode and visible
			ignoreMode |= !Application.isPlaying && isVisible;
#endif

			if (ignoreMode || (UpdateMode == UpdateMode.Always) ||
				(isVisible && (UpdateMode == UpdateMode.WhenVisible)))
			{
				LatticeFeature.Enqueue(this);
			}
		}

		#endregion
	}
}
