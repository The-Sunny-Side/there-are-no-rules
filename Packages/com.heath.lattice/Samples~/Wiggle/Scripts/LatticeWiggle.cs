using UnityEngine;

namespace Lattice.Samples
{
	[RequireComponent(typeof(Lattice))]
	public class LatticeWiggle : MonoBehaviour
	{
		[SerializeField] private float _amount;
		[SerializeField] private float _speed;
		[SerializeField] private float _scale;

		private Lattice _lattice;

		private void Start()
		{
			_lattice = GetComponent<Lattice>();
		}

		private void Update()
		{
			foreach (Vector3Int coords in _lattice.GetHandles())
			{
				Vector3 noise = _amount * GetNoise(
					_scale * (Vector3)coords, 
					_speed * Time.time
				);

				_lattice.SetHandleOffset(coords, noise);
			}
		}

		private static Vector3 GetNoise(Vector3 pos, float t)
		{
			return new Vector3(
				Mathf.PerlinNoise(pos.x + t + 136.23f, pos.y + t),
				Mathf.PerlinNoise(pos.y + t + 51.741f, pos.z + t),
				Mathf.PerlinNoise(pos.z + t + 9.3155f, pos.x + t)
			) - (0.5f * Vector3.one);
		}
	}
}
