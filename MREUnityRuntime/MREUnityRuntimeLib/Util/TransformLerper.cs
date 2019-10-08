// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using UnityEngine;

namespace MixedRealityExtension.Util
{
	/// <summary>
	/// A helper class useful for keeping a transform's position and rotation in sync with that of a transform on another computer.
	/// Idea is that the lerper would receive regular position and/or rotation updates, and would lerp between those updates so that
	/// the player sees smooth animation.
	/// </summary>
	public class TransformLerper
	{
		private readonly Transform _transform;

		private Vector3 _targetPosition;
		private Quaternion _targetRotation;

		private bool _lerpingPosition = false;
		private bool _lerpingRotation = false;

		private Vector3 _startPosition;
		private Quaternion _startRotation;

		private float _startTime;
		private float _updatePeriod;
		private float _percentComplete = 1f;

		/// <summary>
		/// Our default period is based off of the 10hz update cycle that we use for sending actor updates or corrections.
		/// We add in a variance to account for network lag to give the best tuned feel.
		/// </summary>
		private static readonly float DefaultUpdatePeriod = .20f;

		/// <summary>
		/// Initializes and instance of class <see cref="TransformLerper"/>
		/// </summary>
		/// <param name="transform"></param>
		public TransformLerper(Transform transform)
		{
			this._transform = transform;
		}

		/// <summary>
		/// Called to update the target of the lerper for its given transform..
		/// </summary>
		/// <param name="position">The optional new position.</param>
		/// <param name="rotation">The optional new rotation.</param>
		/// <param name="updatePeriod">the expected amount time in seconds, between updates. This is the
		/// time the lerper will take, starting from now, to reach the target position/rotation.</param>
		public void SetTarget(Vector3? position, Quaternion? rotation, float updatePeriod = 0)
		{
			bool canLerp = _transform != null && (position != null || rotation != null);
			_percentComplete = canLerp ? 0f : 1f;
			if (!canLerp)
			{
				return;
			}

			_startTime = Time.timeSinceLevelLoad;
			this._updatePeriod = updatePeriod > 0 ? updatePeriod : DefaultUpdatePeriod;

			if (position.HasValue)
			{
				_targetPosition = position.Value;
				_startPosition = _transform.position;
				_lerpingPosition = true;
			}
			else
			{
				_lerpingPosition = false;
			}

			if (rotation.HasValue)
			{
				_targetRotation = rotation.Value;
				_startRotation = _transform.rotation;
				_lerpingRotation = true;
			}
			else
			{
				_lerpingRotation = false;
			}
		}

		/// <summary>
		/// Clears the target position and/or rotation.
		/// </summary>
		public void ClearTarget()
		{
			SetTarget(null, null);
		}

		/// <summary>
		/// Call this every frame.
		/// Updates the transform position/rotation by one frames-worth of lerping.
		/// </summary>
		public void Update()
		{
			if (_percentComplete < 1f)
			{
				_percentComplete = Mathf.Clamp((Time.timeSinceLevelLoad - _startTime) / _updatePeriod, 0f, 1f);

				if (_lerpingPosition)
				{
					_transform.position = Vector3.Lerp(_startPosition, _targetPosition, _percentComplete);
				}

				if (_lerpingRotation)
				{
					_transform.rotation = Quaternion.LerpUnclamped(_startRotation, _targetRotation, _percentComplete);
				}
			}
		}
	}

}
