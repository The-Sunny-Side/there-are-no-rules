using UnityEngine;

namespace Lattice.Samples
{
	/// <summary>
	/// An example of manually updating a modifier.
	/// </summary>
	public class ManualController : MonoBehaviour
	{
		[SerializeField] private LatticeModifier _modifier;
		[SerializeField] private float _framerate;
		private float _timer;

		private void Update()
		{
			_timer += Time.deltaTime;

			if (_timer > 1 / _framerate)
			{
				_timer = 0;

				// Update deformations this frame
				_modifier.RequestUpdate();
			}
		}
	}
}
