using UnityEngine;

namespace Lattice.Samples
{
	/// <summary>
	/// Controller to move and update lattices with simple spring physics. <br/>
	/// This scripts uses a pool of lattices which it rotates through with each click, 
	/// moving the whole lattice on mouse down, and dragging one of the handles when 
	/// holding down. When released, the handle will use spring physics to move back 
	/// to it's original position within the lattice.
	/// </summary>
	public class GrabController : MonoBehaviour
	{
		/// <summary>
		/// Handle to apply the deformation with.
		/// </summary>
		private readonly Vector3Int Handle = new(0, 0, 0);

		/// <summary>
		/// State of a Lattice's handle.
		/// </summary>
		private struct State
		{
			public Vector3 Position;
			public Vector3 Velocity;
		}

		[SerializeField] private HeadController _headController;
		[SerializeField] private MeshCollider _headCollider;
		[SerializeField] private float _friction;
		[SerializeField] private float _acceleration;
		[SerializeField] private Lattice[] _lattices;

		private int _latticeIndex;
		private bool _grabbing;
		private Plane _grabPlane;
		private Vector3 _grabPosition;
		private Camera _camera;
		private State[] _states;

		private void Start()
		{
			_camera = Camera.main;
			_states = new State[_lattices.Length];
			for (int i = 0; i < _lattices.Length; i++)
			{
				_states[i].Position = _lattices[i].GetHandleBaseWorldPosition(Handle);
			}
		}

		private void Update()
		{
			if (Input.GetMouseButtonDown(0))
			{
				StartGrab();
			}

			if (Input.GetMouseButtonUp(0))
			{
				ReleaseGrab();
			}

			for (int i = 0; i < _lattices.Length; i++)
			{
				UpdateLattice(i);
			}

			if (_grabbing)
			{
				UpdateGrab();
			}
		}

		private void UpdateLattice(int index)
		{
			Lattice lattice = _lattices[index];
			ref State state = ref _states[index];

			// Update lattice handle using spring physics
			Vector3 target = lattice.GetHandleBaseWorldPosition(Handle);
			Vector3 accel = _acceleration * (target - state.Position) - _friction * state.Velocity;

			state.Velocity = state.Velocity + accel          * Time.deltaTime;
			state.Position = state.Position + state.Velocity * Time.deltaTime;

			lattice.SetHandleWorldPosition(Handle, state.Position);
		}

		private void StartGrab()
		{
			Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

			// Check mouse intersection with head
			if (_headCollider.Raycast(ray, out RaycastHit hitInfo, 100f))
			{
				Vector3 position = hitInfo.point;
				Lattice lattice = _lattices[_latticeIndex];
				ref State state = ref _states[_latticeIndex];
				
				// Start grabbing, set initial position to intersection,
				// and create camera facing plane at intersection.
				_grabPosition = position;
				_grabPlane = new Plane(-_camera.transform.forward, _grabPosition);
				_grabbing = true;

				// Move lattice so that handle is at intersection
				Vector3 offset = lattice.transform.TransformVector(-lattice.GetHandleBasePosition(Handle));
				lattice.transform.position = _grabPosition + offset;
				state = new State() { Position = _grabPosition };

				// Start next pose
				_headController.IncrementPose();
			}
		}

		private void UpdateGrab()
		{
			Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

			// Check mouse intersection with camera facing plane
			if (_grabPlane.Raycast(ray, out float dist))
			{
				Vector3 position = ray.GetPoint(dist);
				Lattice lattice = _lattices[_latticeIndex];
				ref State state = ref _states[_latticeIndex];

				// Estimate mouse velocity
				Vector3 velocity = Vector3.Lerp(
					(position - state.Position) / Time.deltaTime,
					state.Velocity,
					0.8f
				);

				// Update lattice handle to match mouse position and velocity
				state = new State()
				{
					Position = position,
					Velocity = velocity
				};

				lattice.SetHandleWorldPosition(Handle, position);

				// Increase pose strength by distance from original location
				float grabDistance = Vector3.Distance(position, _grabPosition);
				_headController.SetPoseStrength(Mathf.Clamp01(3f * grabDistance));
			}
		}

		private void ReleaseGrab()
		{
			// Stop grabbing and use next lattice for next grab
			_grabbing = false;
			_latticeIndex = (_latticeIndex + 1) % _lattices.Length;
			_headController.SetPoseStrength(1f);
		}
	}
}
