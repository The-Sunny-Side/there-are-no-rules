using UnityEngine;

namespace Lattice
{
	/// <summary>
	/// Lattice handle, just a wrapper for a Vector3 offset
	/// </summary>
	[ExecuteAlways, AddComponentMenu("")]
	public class LatticeHandle : MonoBehaviour
	{
		[SerializeField] internal Vector3 offset;

#if UNITY_EDITOR
		private void OnEnable()
		{
			gameObject.hideFlags |= HideFlags.HideInHierarchy;
		}
#endif
	}
}
