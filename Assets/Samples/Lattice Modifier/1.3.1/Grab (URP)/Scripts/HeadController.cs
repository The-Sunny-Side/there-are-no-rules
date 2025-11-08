using UnityEngine;

namespace Lattice.Samples
{
	/// <summary>
	/// Controller to animate the character's face.
	/// </summary>
	public class HeadController : MonoBehaviour
	{
		[SerializeField] private SkinnedMeshRenderer _head;
		[SerializeField] private Transform _leftEye;
		[SerializeField] private Transform _rightEye;
		[SerializeField] private Transform _eyesCentre;
		[SerializeField] private Vector3 _noiseAmount;
		[SerializeField] private Vector3 _noiseSpeed;
		[SerializeField] private SkinnedMeshRenderer[] _poses;

		private Camera _camera;
		private Plane _plane;

		private Vector3 _lookTarget;
		private Vector3 _lookPosition;
		private Vector3 _lookVelocity;

		private Vector3 _facePosition;
		private Vector3 _faceVelocity;

		private int _currentPose;
		private int _blendShapeCount;
		private float[] _blendWeights;

		private float[] _poseTarget;
		private float[] _posePosition;
		private float[] _poseVelocity;

		/// <summary>
		/// Goes to the next pose.
		/// </summary>
		public void IncrementPose()
		{
			_poseTarget[_currentPose] = 0f;
			_currentPose = (_currentPose + 1) % _poses.Length;
		}

		/// <summary>
		/// Sets the pose's strength.
		/// </summary>
		public void SetPoseStrength(float strength)
		{
			_poseTarget[_currentPose] = strength;
		}

		private void Start()
		{
			_camera = Camera.main;
			_plane = new(-Vector3.forward, _eyesCentre.position.z);
			_lookTarget = _camera.transform.position;
			_lookPosition = _lookTarget;
			_facePosition = _lookTarget;

			_blendShapeCount = _head.sharedMesh.blendShapeCount;
			_blendWeights = new float[_blendShapeCount];

			_poseTarget = new float[_poses.Length];
			_posePosition = new float[_poses.Length];
			_poseVelocity = new float[_poses.Length];

			_poseTarget[_currentPose] = 1f;
			_posePosition[_currentPose] = 1f;
		}

		private void Update()
		{
			// Update look target
			Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
			if (_plane.Raycast(ray, out float dist))
			{
				_lookTarget = ray.GetPoint(dist);
			}
			
			// Update the rest
			UpdateHeadLookAt();
			UpdateBlendShapes();
		}

		private void UpdateHeadLookAt()
		{
			// Set eyes to look at target
			_lookPosition = Vector3.SmoothDamp(_lookPosition, _lookTarget, ref _lookVelocity, 0.025f);
			_leftEye.LookAt(_lookPosition);
			_rightEye.LookAt(_lookPosition);

			// Make head slowly look towards target
			_facePosition = Vector3.SmoothDamp(_facePosition, _lookTarget, ref _faceVelocity, 0.5f);
			transform.LookAt(_facePosition - 1f * Vector3.forward);

			// Add noise to head angle
			transform.eulerAngles += new Vector3(
				_noiseAmount.x * (Mathf.PerlinNoise(47.12f, _noiseSpeed.x * Time.time) - 0.5f),
				_noiseAmount.y * (Mathf.PerlinNoise(628.2f, _noiseSpeed.y * Time.time) - 0.5f),
				_noiseAmount.z * (Mathf.PerlinNoise(157.7f, _noiseSpeed.z * Time.time) - 0.5f)
			);
		}

		private void UpdateBlendShapes()
		{
			// Reset to 0
			for (int i = 0; i < _blendShapeCount; i++)
			{
				_blendWeights[i] = 0;
			}

			// Sum all weights
			for (int i = 0; i < _poses.Length; i++)
			{
				_posePosition[i] = Mathf.SmoothDamp(_posePosition[i], _poseTarget[i], ref _poseVelocity[i], 0.2f);
				for (int j = 0; j < _blendShapeCount; j++)
				{
					_blendWeights[j] += _posePosition[i] * _poses[i].GetBlendShapeWeight(j);
				}
			}

			// Apply to renderer
			for (int i = 0; i < _blendShapeCount; i++)
			{
				_head.SetBlendShapeWeight(i, _blendWeights[i]);
			}
		}
	}
}