using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lattice
{
	/// <summary>
	/// A utility component for rebinding an Animator on start.
	/// You will need to do this if you're using a lattice modifier on a skinned mesh with blend shapes controlled by an Animator.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(Animator))]
	public class RebindOnStart : MonoBehaviour
	{
		private const string AnimatorTooltip = 
			"Animator to rebind on start.";

		[SerializeField, Tooltip(AnimatorTooltip)] 
		private Animator _animator;

		void Start()
		{
			if (_animator != null || TryGetComponent(out _animator))
			{
				_animator.Rebind();
			}
		}
	}
}
