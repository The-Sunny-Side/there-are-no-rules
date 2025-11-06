namespace Lattice
{
	/// <summary>
	/// Available update modes on modifiers.
	/// </summary>
	public enum UpdateMode
	{
		/// <summary>
		/// Call <see cref="LatticeModifierBase.RequestUpdate"/> to update deformations when using this.
		/// </summary>
		Manual,

		/// <summary>
		/// Updates when the renderer is visibile.
		/// </summary>
		WhenVisible,

		/// <summary>
		/// Updates every frame.
		/// </summary>
		Always
	}
}
