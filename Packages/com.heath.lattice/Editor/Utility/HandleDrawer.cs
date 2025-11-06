using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.Editor
{
	/// <summary>
	/// Utility class for drawing handles in the scene
	/// </summary>
	public class HandleDrawer
	{
		private readonly Lattice _lattice;
		private readonly SelectedHandles _selectedHandles;

		private readonly List<Handle> _handles = new();

		public HandleDrawer(Lattice lattice, SelectedHandles handles)
		{
			_lattice = lattice;
			_selectedHandles = handles;
		}

		public void Draw(bool drawButtons)
		{
			if (Event.current.type == EventType.Layout)
			{
				SortHandles();
			}
			else if (Event.current.type == EventType.Repaint)
			{
				DrawHandles(0.5f, CompareFunction.Greater);
				DrawHandles(1f, CompareFunction.LessEqual);
			}

			if (drawButtons) DrawButtons();
		}

		private void SortHandles()
		{
			_handles.Clear();

			for (int i = 0; i < _lattice.Resolution.x; i++)
			{
				for (int j = 0; j < _lattice.Resolution.y; j++)
				{
					for (int k = 0; k < _lattice.Resolution.z; k++)
					{
						Vector3Int index = new(i, j, k);
						_handles.Add(new(index, _lattice.GetHandleWorldPosition(index)));
					}
				}
			}

			static int CompareDepth(Handle a, Handle b)
			{
				Vector3 forward = Camera.current.transform.forward;
				float depthA = Vector3.Dot(forward, a.Position);
				float depthB = Vector3.Dot(forward, b.Position);
				float difference = 100 * (depthB - depthA);
				return (int)difference;
			}

			_handles.Sort(CompareDepth);
		}

		private void DrawHandles(float alpha, CompareFunction compareFunction)
		{
			using Handles.DrawingScope drawingScope = new(Matrix4x4.identity);

			CompareFunction previousZTest = Handles.zTest;
			Handles.zTest = compareFunction;

			float handleSize = LatticeSettings.HandleSize * 0.01f;

			// Draw all handles
			for (int i = 0; i < _handles.Count; i++)
			{
				Handle handle = _handles[i];

				float size = handleSize * HandleUtility.GetHandleSize(handle.Position);

				Color color = _selectedHandles.Contains(handle.Index)
					? LatticeSettings.HandleColorSelected
					: LatticeSettings.HandleColor;
				color.a *= alpha;

				using (new Handles.DrawingScope(color))
					Handles.DotHandleCap(0, handle.Position, Quaternion.identity, size, Event.current.type);
			}

			Handles.zTest = previousZTest;
		}

		private void DrawButtons()
		{
			using Handles.DrawingScope drawingScope = new(Color.clear, Matrix4x4.identity);

			CompareFunction previousZTest = Handles.zTest;
			Handles.zTest = CompareFunction.Always;

			float handleSize = LatticeSettings.HandleSize * 0.01f;

			for (int i = 0; i < _handles.Count; i++)
			{
				Handle handle = _handles[i];
				float size = handleSize * HandleUtility.GetHandleSize(handle.Position);

				// If clicked on, update selected list
				if (Handles.Button(handle.Position, Quaternion.identity, size, size * 1.5f, Handles.DotHandleCap))
				{
					Undo.RecordObject(_selectedHandles, "Select Lattice Handle");
					if (Event.current.shift)
					{
						if (_selectedHandles.Contains(handle.Index)) _selectedHandles.Remove(handle.Index);
						else _selectedHandles.Add(handle.Index);
					}
					else
					{
						_selectedHandles.Clear();
						_selectedHandles.Add(handle.Index);
					}
				}
			}

			Handles.zTest = previousZTest;
		}

		struct Handle
		{
			public Vector3Int Index;
			public Vector3 Position;

			public Handle(Vector3Int index, Vector3 position)
			{
				Index = index;
				Position = position;
			}
		}
	}
}
