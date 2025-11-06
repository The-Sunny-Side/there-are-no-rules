using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lattice
{
	/// <summary>
	/// Useful lattice extension methods.
	/// </summary>
	public static class LatticeExtensions
	{
		/// <summary>
		/// Gets a enumerable struct to help iterate over all lattice handles.
		/// </summary>
		public static Handles GetHandles(this Lattice lattice)
		{
			return new Handles(lattice);
		}

		/// <summary>
		/// Transforms the lattice to contain all renderers of a transform.
		/// </summary>
		public static void FitToTransform(this Lattice lattice, Transform target, bool includeChildren = true, bool rotateLattice = false)
		{
			Renderer[] renderers = includeChildren
				? target.GetComponentsInChildren<Renderer>()
				: target.GetComponents<Renderer>();

			if (renderers.Length == 0) return;

			static Bounds GetBounds(Matrix4x4 worldToLocal, Renderer renderer)
			{
				Matrix4x4 rendererToLocal = worldToLocal * renderer.localToWorldMatrix;

				if ((renderer is SkinnedMeshRenderer skinnedRenderer) && (skinnedRenderer.rootBone != null))
				{
					rendererToLocal = worldToLocal * skinnedRenderer.rootBone.localToWorldMatrix;
				}

				Bounds rendererBounds = renderer.localBounds;
				Bounds bounds = new(rendererToLocal.MultiplyPoint(rendererBounds.min), Vector3.zero);

				for (int x = 0; x <= 1; x++)
				{
					for (int y = 0; y <= 1; y++)
					{
						for (int z = 0; z <= 1; z++)
						{
							if (x == 0 && y == 0 && z == 0) continue;

							Vector3 offset = new(
								x * rendererBounds.size.x,
								y * rendererBounds.size.y,
								z * rendererBounds.size.z
							);
							Vector3 point = rendererBounds.min + offset;
							bounds.Encapsulate(rendererToLocal.MultiplyPoint(point));
						}
					}
				}

				return bounds;
			}

			// Clear initial transform
			Transform transform = lattice.transform;
			if (rotateLattice) transform.rotation = target.rotation;
			transform.localScale = Vector3.one;

			// Calculate bounds in transform space
			Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
			Bounds bounds = GetBounds(worldToLocal, renderers[0]);
			for (int i = 1; i < renderers.Length; i++)
			{
				bounds.Encapsulate(GetBounds(worldToLocal, renderers[i]));
			}

			// Move to fit bounds
			transform.position = transform.TransformPoint(bounds.center);
			transform.localScale = bounds.size;
		}

		#region Enumeration

		/// <summary>
		/// Utility struct to help iterate over all handles in a lattice, without having nested loops.
		/// </summary>
		public struct Handles
		{
			public struct Enumerator
			{
				private Lattice _lattice;
				private Vector3Int _index;

				public readonly Vector3Int Current => _index;

				public Enumerator(Lattice lattice)
				{
					_lattice = lattice;
					_index = new(0, 0, -1);
				}

				public bool MoveNext()
				{
					_index.z += 1;

					if (_index.z >= _lattice.Resolution.z)
					{
						_index.z = 0;
						_index.y += 1;

						if (_index.y >= _lattice.Resolution.y)
						{
							_index.y = 0;
							_index.x += 1;

							if (_index.x >= _lattice.Resolution.x)
							{
								return false;
							}
						}
					}

					return true;
				}
			}

			private readonly Lattice _lattice;

			public readonly Enumerator GetEnumerator() => new(_lattice);

			public Handles(Lattice lattice)
			{
				_lattice = lattice;
			}
		}

		#endregion
	}
}
