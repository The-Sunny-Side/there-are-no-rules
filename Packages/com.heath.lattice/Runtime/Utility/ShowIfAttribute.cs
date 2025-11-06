using UnityEngine;

namespace Lattice
{
	/// <summary>
	/// Utility for editors to hide fields under certain conditions. <br/>
	/// Only implemented on types used in this package. This is not a general purpose solution.
	/// </summary>
	public class ShowIfAttribute : PropertyAttribute
	{
		/// <summary>
		/// Field name to check.
		/// </summary>
		public string Name;

		/// <summary>
		/// Expected value. Will hide the field if it does not match this value.
		/// </summary>
		public object[] Values = new object[0];

		public ShowIfAttribute(string name, params object[] values)
		{
			Name = name;
			Values = values;
		}
	}
}
