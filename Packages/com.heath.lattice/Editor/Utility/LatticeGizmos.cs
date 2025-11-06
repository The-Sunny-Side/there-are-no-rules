using UnityEditor;
using UnityEngine;

namespace Lattice.Editor
{
	/// <summary>
	/// Utility class for drawing gizmos to move, rotate and scale lattices
	/// </summary>
	public class LatticeGizmos
	{
		private readonly Lattice _lattice;

		private Quaternion _previousToolRotation = Quaternion.identity;
		private Vector3 _previousToolScale = Vector3.one;

		public LatticeGizmos(Lattice lattice)
		{
			_lattice = lattice;
			Reset();
		}

		public void Reset()
		{
			_previousToolScale = Vector3.one;
			_previousToolRotation = (Tools.pivotRotation == PivotRotation.Global)
				? Quaternion.identity
				: _lattice.transform.rotation;
		}

		public void Draw()
		{
			if (Event.current.type == EventType.MouseUp) Reset();

			if (Tools.current == Tool.Move) DrawPositionGizmo();
			else if (Tools.current == Tool.Rotate) DrawRotationGizmo();
			else if (Tools.current == Tool.Scale) DrawScaleGizmo();
		}

		private void DrawPositionGizmo()
		{
			Quaternion rotation = (Tools.pivotRotation == PivotRotation.Global)
				? Quaternion.identity
				: _lattice.transform.rotation;

			EditorGUI.BeginChangeCheck();

			Vector3 finalPosition = Handles.PositionHandle(_lattice.transform.position, rotation);

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(_lattice.transform, "Moved Lattice");
				_lattice.transform.position = finalPosition;
			}
		}

		private void DrawRotationGizmo()
		{
			EditorGUI.BeginChangeCheck();

			Quaternion finalRotation = Handles.RotationHandle(_previousToolRotation, _lattice.transform.position);

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(_lattice.transform, "Rotated Lattice");
				Quaternion change = Quaternion.Inverse(_previousToolRotation) * finalRotation;
				_previousToolRotation = finalRotation;

				if (Tools.pivotRotation == PivotRotation.Global)
				{
					_lattice.transform.rotation = change * _lattice.transform.rotation;
				}
				else
				{
					_lattice.transform.rotation = _lattice.transform.rotation * change;
				}
			}
		}

		private void DrawScaleGizmo()
		{
			EditorGUI.BeginChangeCheck();

			Vector3 finalScale = Handles.ScaleHandle(_previousToolScale, _lattice.transform.position, _lattice.transform.rotation);

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(_lattice.transform, "Scaled Lattice");
				Vector3 change = new(
					finalScale.x / _previousToolScale.x,
					finalScale.y / _previousToolScale.y,
					finalScale.z / _previousToolScale.z
				);
				_previousToolScale = finalScale;

				Vector3 scale = _lattice.transform.localScale;
				scale.Scale(change);
				_lattice.transform.localScale = scale;
			}
		}
	}
}
