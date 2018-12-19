// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Animation
{
    /// <summary>
    /// Class that represents the state of an animation.
    /// </summary>
    public class MWAnimationState
    {
        /// <summary>
        /// The id of the actor of the animation.
        /// </summary>
        public Guid ActorId;

        /// <summary>
        /// The name of the animation.
        /// </summary>
        public string AnimationName;

        /// <summary>
        /// The current time offset of the animation (in seconds).
        /// </summary>
        public float AnimationTime;

        /// <summary>
        /// Whether or not the animation is paused.
        /// </summary>
        public bool Paused;

        /// <summary>
        /// Whether or not to the animation should apply root motion when stopped/restarted.
        /// </summary>
        public bool HasRootMotion;
    }
}
