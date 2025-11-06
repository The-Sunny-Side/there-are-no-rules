using UnityEngine;

namespace Lattice.Samples
{
	[ExecuteAlways]
	public class LatticeController : MonoBehaviour
	{
		[SerializeField] private Lattice _lattice;

		private void Update()
		{
			if (_lattice == null) return;

			foreach (Vector3Int handle in _lattice.GetHandles())
			{
				Vector3 localPosition = _lattice.GetHandleBasePosition(handle);
				Vector3 worldPosition = transform.TransformPoint(localPosition);
				_lattice.SetHandleWorldPosition(handle, worldPosition);
			}
		}
	}
}
