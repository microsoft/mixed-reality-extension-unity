using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Util.Unity;
using UnityEngine;

namespace MixedRealityExtension.Util
{/// <summary>
    /// A helper class useful for keeping a transform's position and rotation in sync with that of a transform on another computer.
    /// Idea is that the lerper would receive regular position and/or rotation updates, and would lerp between those updates so that
    /// the player sees smooth animation.
    ///
    /// Inspired by InterpolatedVector3 and InterpolatedQuaternion
    ///
    /// See PhysicalToolBehaviour for example usage.
    /// </summary>
    [System.Reflection.Obfuscation(Exclude = false)]
    public class TransformLerper
    {
        private readonly Transform transform;

        private Vector3? targetPosition;
        private Quaternion? targetRotation;

        private Vector3 startPosition;
        private Quaternion startRotation;

        private float startTime;
        private float updatePeriod;
        private float percentComplete = 1f;

        /// <summary>
        /// Our default period is based off of the 10hz update cycle that we use for sending actor updates or corrections.
        /// </summary>
        private static readonly float DefaultUpdatePeriod = .1f;

        public TransformLerper(Transform transform)
        {
            this.transform = transform;
        }

        /// <summary>
        /// Call this to set the new position and rotation target when a new update is received over the network.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="updatePeriod">the expected amount time in seconds, between updates. This is the
        /// time the lerper will take, starting from now, to reach the target position/rotation.
        /// Default is PhotonNetork.serializationPeriod</param>
        public void SetTarget(MWVector3 position, MWQuaternion rotation, float updatePeriod = 0)
        {
            bool canLerp = transform != null && (position != null || rotation != null);
            percentComplete = canLerp ? 0f : 1f;
            if (!canLerp) return;

            startTime = Time.timeSinceLevelLoad;
            this.updatePeriod = updatePeriod > 0 ? updatePeriod : DefaultUpdatePeriod;

            targetPosition = targetPosition ?? new Vector3();
            targetPosition?.SetValue(position);
            if (targetPosition.HasValue) startPosition = transform.position;

            targetRotation = targetRotation ?? new Quaternion();
            targetRotation?.SetValue(rotation);
            if (targetRotation.HasValue) startRotation = transform.rotation;
        }

        public void ClearTarget()
        {
            SetTarget(null, null);
        }

        /// <summary>
        /// Call this every frame.
        /// Updates the transform position/rotation by one frames-worth of lerping.
        /// </summary>
        public void LerpIfNeeded()
        {
            if (percentComplete < 1f)
            {
                percentComplete = Mathf.Clamp((Time.timeSinceLevelLoad - startTime) / updatePeriod, 0f, 1f);

                if (targetPosition.HasValue) transform.position = Vector3.Lerp(startPosition, targetPosition.Value, percentComplete);
                if (targetRotation.HasValue) transform.rotation = Quaternion.LerpUnclamped(startRotation, targetRotation.Value, percentComplete);
            }
        }
    }

}
