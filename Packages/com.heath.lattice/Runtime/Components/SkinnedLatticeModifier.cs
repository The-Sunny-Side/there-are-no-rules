using System.Collections.Generic;
using UnityEngine;

namespace Lattice
{
	/// <summary>
	/// Lattice modifier for skinned mesh renderers.
	/// </summary>
	[ExecuteAlways] 
	[DisallowMultipleComponent] 
	[RequireComponent(typeof(SkinnedMeshRenderer))]
	public class SkinnedLatticeModifier : LatticeModifierBase
	{
		#region Constants

		private const string SkinnedLatticesTooltip =
			"Lattices to apply to the target mesh. " +
			"These will be applied in order and after skinning.";

		#endregion

		#region Fields

		[SerializeField, Tooltip(SkinnedLatticesTooltip)]
		private List<LatticeItem> _skinnedLattices = new()
		{
			new LatticeItem()
			{
				Mask = { Vertex = { Multiplier = 1f } }
			}
		};

		private SkinnedMeshRenderer _skinnedMeshRenderer;
		private GraphicsBuffer _skinnedVertexBuffer;
		private Matrix4x4 _skinnedLocalToWorld;

		#endregion

		#region Properties

		/// <summary>
		/// Skinned lattices to apply.
		/// </summary>
		public List<LatticeItem> SkinnedLattices => _skinnedLattices;

		/// <summary>
		/// Gets the current skinned local to world matrix.
		/// </summary>
		internal Matrix4x4 SkinnedLocalToWorld => _skinnedLocalToWorld;

		/// <summary>
		/// Retrieves the skinned mesh renderer.
		/// </summary>
		private SkinnedMeshRenderer MeshRenderer => (_skinnedMeshRenderer == null)
			? _skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>()
			: _skinnedMeshRenderer;

		#endregion

		#region Protected Methods

		/// <inheritdoc cref="LatticeModifierBase.GetMesh"/>
		protected override Mesh GetMesh()
		{
			return MeshRenderer.sharedMesh;
		}

		/// <inheritdoc cref="LatticeModifierBase.SetMesh"/>
		protected override void SetMesh(Mesh mesh)
		{
#if UNITY_EDITOR
			// Ensure GPU skinning is enabled or report error.
			CheckGpuSkinning();
#endif
			// Apply mesh
			MeshRenderer.sharedMesh = mesh;

			// Update the renderer to have at least one bone
			// This will ensure it uses the correct vertex buffer when rendering
			if (MeshRenderer.bones.Length == 0)
			{
				MeshRenderer.bones = new[] { transform };
			}
		}

		/// <inheritdoc cref="LatticeModifierBase.Release"/>
		protected override void Release()
		{
			base.Release();

			_skinnedVertexBuffer?.Release();
			_skinnedVertexBuffer = null;
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

			if (ignoreMode || isVisible || MeshRenderer.updateWhenOffscreen)
			{
				Transform root = (MeshRenderer.rootBone != null)
					? MeshRenderer.rootBone 
					: transform;

				_skinnedLocalToWorld = Matrix4x4.TRS(root.position,
					root.rotation, Vector3.one);

				LatticeFeature.EnqueueSkinned(this);
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Trys to get the current skinned vertex buffer.
		/// </summary>
		internal bool TryGetSkinnedBuffer(out GraphicsBuffer skinnedBuffer)
		{
			// Ideally you cache the vertex buffer without releasing it every frame
			// But the skin renderer may swap to a new vertex buffer without disposing the previous
			// So no way to tell if it swapped within code :(
			_skinnedVertexBuffer?.Release();
			_skinnedVertexBuffer = null;

			// Update skinned vertex buffer
			_skinnedMeshRenderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
			_skinnedVertexBuffer = _skinnedMeshRenderer.GetVertexBuffer();

			skinnedBuffer = _skinnedVertexBuffer;
			return skinnedBuffer != null;
		}

		#endregion

		#region Editor Only

#if UNITY_EDITOR

		/// <summary>
		/// Message to print if GPU Skinning is not enabled.
		/// </summary>
		private const string GpuSkinningError =
			"GPU Skinning has not been enabled in the Player Settings! Skinned Lattice Modifiers will not be applied.\n" +
			"Open the Player Settings (Edit > Project Settings... > Player) and ensure GPU Skinning is enabled or set to GPU/GPU Batched.";

		/// <summary>
		/// Checks that GPU Skinning is enabled or otherwise logs an error.
		/// </summary>
		private static void CheckGpuSkinning()
		{
			if (!UnityEditor.PlayerSettings.gpuSkinning)
			{
				Debug.LogError(GpuSkinningError);
			}
		}
#endif

		#endregion
	}
}
