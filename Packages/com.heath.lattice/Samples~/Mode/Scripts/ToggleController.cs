using UnityEngine;

namespace Lattice.Samples
{
	/// <summary>
	/// An example of setting UpdateMode of a modifier.
	/// </summary>
	public class ToggleController : MonoBehaviour
	{
		[SerializeField] private LatticeModifier _modifier;
		[SerializeField] private float _duration;
		private float _timer;
		private bool _mode;

		private void Update()
		{
			_timer += Time.deltaTime;

			if (_timer > _duration)
			{
				_timer = 0;
				_mode = !_mode;

				// Switch update mode
				_modifier.UpdateMode = _mode
					? UpdateMode.Manual
					: UpdateMode.WhenVisible;
			}
		}
	}
}
