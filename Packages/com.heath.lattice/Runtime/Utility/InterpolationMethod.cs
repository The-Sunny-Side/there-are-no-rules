using UnityEngine;

namespace Lattice
{
	/// <summary>
	/// Interpolation method.
	/// </summary>
	public enum InterpolationMethod
	{
		/// <summary>
		/// Trilinear without smoothing.
		/// </summary>
		LinearSharp = 1,

		/// <summary>
		/// Trilinear with smoothing.
		/// </summary>
		LinearSmooth = 0,

		/// <summary>
		/// Tricubic.
		/// </summary>
		Cubic = 2,
	}
}
