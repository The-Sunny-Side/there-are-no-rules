using UnityEngine;

namespace Lattice
{
	/// <summary>
	/// Method to apply deformations to a mesh with.
	/// </summary>
	public enum ApplyMethod
	{
		/// <summary>
		/// Only edit the position of a vertex.
		/// </summary>
		PositionOnly,

		/// <summary>
		/// Edit the position, normal and tangent.
		/// </summary>
		[InspectorName("Position, Normal and Tangent")]
		PositionNormalTangent,

		/// <summary>
		/// Edit the position, normal, tangent, and calculate squish and stretch.
		/// </summary>
		Stretch,
	};
}