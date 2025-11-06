using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lattice.Samples
{
	/// <summary>
	/// An example of controlling a lattice to follow a sine wave.
	/// </summary>
	public class DrapesLatticeController : MonoBehaviour
	{
		[SerializeField] private float _speed;
		[SerializeField] private float _scale;
		[SerializeField] private float _distance;
		private Lattice _lattice;

		private void OnEnable()
		{
			_lattice = GetComponent<Lattice>();
		}

		private void Update()
		{
			foreach (Vector3Int handle in _lattice.GetHandles())
			{
				// Determine offset amount
				float position = _scale * handle.x;
				float wind = Mathf.Sin(_speed * Time.timeSinceLevelLoad + position);
				float offset = _distance * handle.y * wind;

				// Set handle offset
				_lattice.SetHandleOffset(handle, new Vector3(0, 0, offset));
			}
		}
	}
}
