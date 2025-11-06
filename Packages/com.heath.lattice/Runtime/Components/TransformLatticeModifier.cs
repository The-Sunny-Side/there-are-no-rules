using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lattice
{
	/// <summary>
	/// Lattice modifier for transform components.
	/// </summary>
	[ExecuteAlways]
	public class TransformLatticeModifier : MonoBehaviour
	{
		/// <summary>
		/// Components of the transform which will be deformed.
		/// </summary>
		[Flags]
		public enum ApplyFlags
		{
			Position = 1,
			Rotation = 2,
			Scale = 4,
		};

		private const string TargetTransformTooltip = 
			"The transform to apply deformations to.";

		private const string ApplyMethodTooltip = 
			"Which components of the transform to apply deformations to.";

		private const string LatticesTooltip = 
			"Lattices to apply to the transform.";

		[SerializeField, Tooltip(TargetTransformTooltip)] 
		private Transform _targetTransform;

		[SerializeField, Tooltip(ApplyMethodTooltip)] 
		private ApplyFlags _applyMethod = ApplyFlags.Position | ApplyFlags.Rotation;

		[SerializeField, Tooltip(LatticesTooltip)] 
		private List<LatticeItem> _lattices = new()
		{
			new LatticeItem()
			{
				Interpolation = InterpolationMethod.Cubic,
				Mask = { Vertex = { Multiplier = 1f } }
			}
		};

		/// <summary>
		/// The transform before deforming.
		/// </summary>
		public Transform TargetTransform
		{ 
			get => _targetTransform; 
			set => _targetTransform = value; 
		}
		
		/// <summary>
		/// Which components will be deformed.
		/// </summary>
		public ApplyFlags ApplyMethod
		{ 
			get => _applyMethod; 
			set => _applyMethod = value; 
		}

		/// <summary>
		/// The lattices to be applied.
		/// </summary>
		public List<LatticeItem> Lattices => _lattices;

		private void LateUpdate()
		{
			if (_targetTransform == null) 
				return;

			// Get target matrix
			Matrix4x4 matrix = _targetTransform.localToWorldMatrix;

			// Apply lattices
			for (int i = 0; i < _lattices.Count; i++)
			{
				Lattice lattice = _lattices[i].Lattice;

				if ((lattice == null) || !lattice.isActiveAndEnabled) 
					continue;

				matrix = LatticeSolver.DeformTransform(_lattices[i], matrix);
			}

			// Apply postion
			if (_applyMethod.HasFlag(ApplyFlags.Position)) transform.position = matrix.GetPosition();
			else transform.position = _targetTransform.position;

			// Apply rotation
			if (_applyMethod.HasFlag(ApplyFlags.Rotation)) transform.rotation = matrix.rotation;
			else transform.rotation = _targetTransform.rotation;

			// Apply scale
			Matrix4x4 scaleMatrix = _applyMethod.HasFlag(ApplyFlags.Scale) ? matrix : _targetTransform.localToWorldMatrix;
			transform.localScale = Vector3.one;
			transform.localScale = (transform.worldToLocalMatrix * scaleMatrix).lossyScale;
		}
	}
}
