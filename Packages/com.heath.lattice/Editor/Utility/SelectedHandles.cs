using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lattice.Editor
{
	/// <summary>
	/// Utility class for storing selected handles. <br/>
	/// Works with undo and redo and between <see cref="LatticeEditor"/> instances
	/// </summary>
	[Serializable]
	public class SelectedHandles : ScriptableObject
	{
		private static readonly Dictionary<Lattice, SelectedHandles> _instances = new();

		public static SelectedHandles Get(Lattice lattice)
		{
			if (!_instances.TryGetValue(lattice, out var instance))
			{
				instance = CreateInstance<SelectedHandles>();
				instance.name = "Selected Handles";
				instance.hideFlags = HideFlags.DontSave;
				_instances[lattice] = instance;
			}
			instance._lattice = lattice;
			return instance;
		}

		public event Action SelectionChanged;

		[SerializeField] private List<Vector3Int> _handles = new();
		private Lattice _lattice;

		public IReadOnlyList<Vector3Int> Handles
		{
			get
			{
				Validate();
				return _handles;
			}
		}

		public int Count
		{
			get
			{
				Validate();
				return _handles.Count;
			}
		}

		public void Add(Vector3Int handle)
		{
			if (!_handles.Contains(handle))
			{
				_handles.Add(handle);
				SelectionChanged?.Invoke();
			}
		}

		public void AddRange(IEnumerable<Vector3Int> handles)
		{
			foreach (Vector3Int handle in handles)
			{
				Add(handle);
			}
		}

		public void Remove(Vector3Int handle)
		{
			_handles.Remove(handle);
			SelectionChanged?.Invoke();
		}

		public void Clear()
		{
			_handles.Clear();
			SelectionChanged?.Invoke();
		}

		public bool Contains(Vector3Int handle)
		{
			return _handles.Contains(handle);
		}

		public Vector3 GetPivot()
		{
			Validate();

			if (_handles.Count == 0) return default;

			if (Tools.pivotMode == PivotMode.Center)
			{
				Vector3 centre = Vector3.zero;
				for (int i = 0; i < _handles.Count; i++)
				{
					centre += _lattice.GetHandleWorldPosition(_handles[i]);
				}
				centre /= Count;
				return centre;
			}
			else
			{
				return _lattice.GetHandleWorldPosition(_handles[^1]);
			}
		}

		public Bounds GetBounds()
		{
			Validate();

			if (_handles.Count == 0) return default;

			Vector3 initial = _lattice.GetHandleWorldPosition(_handles[0]);

			if (_handles.Count == 1)
			{
				Vector3 scale = new(
					1f / (_lattice.Resolution.x - 1), 
					1f / (_lattice.Resolution.y - 1), 
					1f / (_lattice.Resolution.z - 1)
				);
				scale = _lattice.transform.TransformVector(scale);
				return new Bounds(initial, scale);
			}

			Bounds bounds = new(initial, Vector3.zero);
			for (int i = 1; i < _handles.Count; i++)
			{
				bounds.Encapsulate(_lattice.GetHandleWorldPosition(_handles[i]));
			}
			return bounds;
		}

		private void Validate()
		{
			foreach (Vector3Int handle in _handles)
			{
				if ((handle.x >= _lattice.Resolution.x) ||
					(handle.y >= _lattice.Resolution.y) ||
					(handle.z >= _lattice.Resolution.z))
				{
					Clear();
					return;
				}
			}
		}
	}
}
