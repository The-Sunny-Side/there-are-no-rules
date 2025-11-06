using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace Lattice
{
	/// <summary>
	/// The lattice componenet, a 3d grid of of deformation handles.
	/// </summary>
	[ExecuteAlways]
	public class Lattice : MonoBehaviour
	{
		#region Constants

		private const string ResolutionTooltip = 
			"The number of handles along each axis.";

		#endregion

		#region Fields

		[SerializeField, NotKeyable, Tooltip(ResolutionTooltip)] 
		private Vector3Int _resolution = new(2, 2, 2);

		[SerializeField, HideInInspector] 
		private List<LatticeHandle> _handles = new();

		private readonly List<Vector3> _offsets = new();

		#endregion

		#region Properties

		/// <summary>
		/// The resolution of the lattice.<br/>
		/// To change this use <see cref="Setup(Vector3Int)"/>
		/// </summary>
		public Vector3Int Resolution => _resolution;

		/// <summary>
		/// The offsets to be used in deformation.<br/>
		/// These are automatically updated as part of <see cref="LateUpdate"/>.<br/>
		/// To manually set them use <see cref="SetHandleOffset(int, int, int, Vector3)"/>
		/// </summary>
		internal List<Vector3> Offsets
		{
			get
			{
				ValidateOffsets();
				return _offsets;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Gets the current offset from the handle's base position.
		/// </summary>
		public Vector3 GetHandleOffset(int x, int y, int z)
		{
			return _handles[GetIndex(x, y, z)].offset;
		}

		/// <inheritdoc cref="GetHandleOffset(int, int, int)"/>
		public Vector3 GetHandleOffset(Vector3Int coords) => GetHandleOffset(coords.x, coords.y, coords.z);

		/// <summary>
		/// Set the offset of a handle relative to its base position.
		/// </summary>
		public void SetHandleOffset(int x, int y, int z, Vector3 offset)
		{
			_handles[GetIndex(x, y, z)].offset = offset;
		}

		/// <inheritdoc cref="SetHandleOffset(int, int, int, Vector3)"/>
		public void SetHandleOffset(Vector3Int coords, Vector3 offset) => SetHandleOffset(coords.x, coords.y, coords.z, offset);

		/// <summary>
		/// Gets the position of a handle including offset in local space.
		/// </summary>
		public Vector3 GetHandlePosition(int x, int y, int z)
		{
			return GetHandleOffset(x, y, z) + GetHandleBasePosition(x, y, z);
		}

		/// <inheritdoc cref="GetHandlePosition(int, int, int)"/>
		public Vector3 GetHandlePosition(Vector3Int coords) => GetHandlePosition(coords.x, coords.y, coords.z);

		/// <summary>
		/// Set the position of a handle in local space.
		/// </summary>
		public void SetHandlePosition(int x, int y, int z, Vector3 position)
		{
			SetHandleOffset(x, y, z, position - GetHandleBasePosition(x, y, z));
		}

		/// <inheritdoc cref="SetHandlePosition(int, int, int, Vector3)"/>
		public void SetHandlePosition(Vector3Int coords, Vector3 position) => SetHandlePosition(coords.x, coords.y, coords.z, position);

		/// <summary>
		/// Get the position of a handle include offset in world space.
		/// </summary>
		public Vector3 GetHandleWorldPosition(int x, int y, int z)
		{
			return transform.TransformPoint(GetHandlePosition(x, y, z));
		}

		/// <inheritdoc cref="GetHandleBasePosition(int, int, int)"/>
		public Vector3 GetHandleWorldPosition(Vector3Int coords) => GetHandleWorldPosition(coords.x, coords.y, coords.z);

		/// <summary>
		/// Set the position of a handle in world space.
		/// </summary>
		public void SetHandleWorldPosition(int x, int y, int z, Vector3 position)
		{
			SetHandlePosition(x, y, z, transform.InverseTransformPoint(position));
		}

		/// <inheritdoc cref="SetHandleWorldPosition(int, int, int, Vector3)"/>
		public void SetHandleWorldPosition(Vector3Int coords, Vector3 position) => SetHandleWorldPosition(coords.x, coords.y, coords.z, position);

		/// <summary>
		/// Gets the local position of a handle before any offset in local space.
		/// </summary>
		public Vector3 GetHandleBasePosition(int x, int y, int z)
		{
			return new Vector3(
				x / (_resolution.x - 1f) - 0.5f,
				y / (_resolution.y - 1f) - 0.5f,
				z / (_resolution.z - 1f) - 0.5f
			);
		}

		/// <inheritdoc cref="GetHandleBasePosition(int, int, int)"/>
		public Vector3 GetHandleBasePosition(Vector3Int coords) => GetHandleBasePosition(coords.x, coords.y, coords.z);

		/// <summary>
		/// Gets the position of a handle before any offsets in world space.
		/// </summary>
		public Vector3 GetHandleBaseWorldPosition(int x, int y, int z)
		{
			return transform.TransformPoint(GetHandleBasePosition(x, y, z));
		}

		/// <inheritdoc cref="GetHandleBaseWorldPosition(int, int, int)"/>
		public Vector3 GetHandleBaseWorldPosition(Vector3Int coords) => GetHandleBaseWorldPosition(coords.x, coords.y, coords.z);

		/// <summary>
		/// Sets up the lattice for the desired resolution.
		/// </summary>
		/// <param name="resolution">The desired resolution</param>
		public void Setup(Vector3Int resolution)
		{
			// Clamp resolution, cannot be less than 2 on each axis
			resolution = Vector3Int.Max(2 * Vector3Int.one, resolution);

			// Delete existing handles
			LatticeHandle[] existingHandles = GetComponentsInChildren<LatticeHandle>();
			for (int i = 0; i < existingHandles.Length; i++)
			{
				GameObject child = existingHandles[i].gameObject;

				// Don't delete if it belongs to a child lattice
				if (child.transform.parent != transform) 
					continue;

				if (Application.isPlaying)
				{
					Destroy(child);
				}
				else
				{
#if UNITY_EDITOR
					UnityEditor.Undo.DestroyObjectImmediate(child);
#endif
				}
			}

#if UNITY_EDITOR
			UnityEditor.Undo.RecordObject(this, "Setup");
#endif
			// Update resolution
			_resolution = resolution;

			// Create new handles
			_handles.Clear();
			for (int k = 0; k < _resolution.z; k++)
			{
				for (int j = 0; j < _resolution.y; j++)
				{
					for (int i = 0; i < _resolution.x; i++)
					{
						GameObject childObject = new($"Handle ({i}, {j}, {k})");
						childObject.transform.parent = transform;
						childObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

						LatticeHandle handle = childObject.AddComponent<LatticeHandle>();
						_handles.Add(handle);
#if UNITY_EDITOR
						UnityEditor.Undo.RegisterCreatedObjectUndo(childObject, "Create Handle");
#endif
					}
				}
			}
#if UNITY_EDITOR
			UnityEditor.Undo.RegisterCompleteObjectUndo(this, "Setup");
#endif
		}

		/// <inheritdoc cref="Setup(Vector3Int)"/>
		public void Setup(int x, int y, int z) => Setup(new Vector3Int(x, y, z));

		/// <summary>
		/// Gets the handle component by index.
		/// </summary>
		internal LatticeHandle GetHandle(int x, int y, int z)
		{
			return _handles[GetIndex(x, y, z)];
		}

		/// <inheritdoc cref="GetHandle(int, int, int)"/>
		internal LatticeHandle GetHandle(Vector3Int coords) => GetHandle(coords.x, coords.y, coords.z);

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the array index from a 3d handle index.
		/// </summary>
		private int GetIndex(int x, int y, int z)
		{
			return x + (_resolution.x * y) + (_resolution.x * _resolution.y * z);
		}

		/// <summary>
		/// Ensures offsets are correct.
		/// </summary>
		private void ValidateOffsets()
		{
			if (_offsets.Count != _handles.Count)
			{
				_offsets.Clear();
				for (int i = 0; i < _handles.Count; i++)
				{
					_offsets.Add(_handles[i].offset);
				}
			}
		}

		#endregion

		#region Unity Methods

		private void Awake()
		{
			if (_handles.Count != _resolution.x * _resolution.y * _resolution.z)
			{
				Setup(_resolution);
			}
		}

		private void LateUpdate()
		{
			ValidateOffsets();
			for (int i = 0; i < _handles.Count; i++)
			{
				_offsets[i] = _handles[i].offset;
			}
		}

		private void OnValidate()
		{
			_resolution = Vector3Int.Max(2 * Vector3Int.one, _resolution);
		}

		#endregion
	}
}
