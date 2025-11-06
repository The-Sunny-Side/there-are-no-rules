using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lattice
{
	/// <summary>
	/// Used to reference Lattices with per modifier settings.
	/// </summary>
	[Serializable]
	public struct LatticeItem : ISerializationCallbackReceiver
	{
		private const string LatticeTooltip = "Lattice to apply.";

		private const string InterpolationTooltip =
			"Method of interpolation:\n" +
			" - Linear Sharp:  Linear interpolation.\n" +
			" - Linear Smooth:  Linear interpolation, with approximate smoothing.\n" +
			" - Cubic:  Cubic interpolation, looks better than linear but is more expensive.";

		private const string GlobalTooltip =
			"How deformation outside the lattice is handled:\n" +
			" - Disabled:  Deformation tapers off outside the lattice.\n" +
			" - Enabled:  Deformation continues outside the lattice to match the outer handles.";

		private const string MaskTooltip =
			"Masking options. Can be used to control the deformation over a mesh.";

		[Tooltip(LatticeTooltip)]
		public Lattice Lattice;

		[Tooltip(InterpolationTooltip)]
		public InterpolationMethod Interpolation;

		[Tooltip(GlobalTooltip)]
		public bool Global;

		[Tooltip(MaskTooltip)]
		public LatticeMask Mask;

		#region Serialization

		[SerializeField, HideInInspector, FormerlySerializedAs("HighQuality")]
		private int d_HighQuality; // Deprecated high quality field

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			d_HighQuality = -1;
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			// Upgrading from a version which used the high quality field
			if ((d_HighQuality >= 0) && (d_HighQuality <= 1))
			{
				Interpolation = (d_HighQuality == 1) 
					? InterpolationMethod.Cubic 
					: InterpolationMethod.LinearSmooth;
			}
		}

		#endregion
	}
}
