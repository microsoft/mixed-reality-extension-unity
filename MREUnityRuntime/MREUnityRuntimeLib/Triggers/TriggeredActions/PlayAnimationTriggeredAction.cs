// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;

namespace MixedRealityExtension.Triggers.TriggeredActions
{
    /// <summary>
    /// Class that plays an animation in response to a trigger being fired.
    /// </summary>
    public class PlayAnimationTriggeredAction : TriggeredActionBase
    {
        /// <summary>
        /// The name of the animation to be played.
        /// </summary>
        public string AnimationName { get; set; }

        /// <summary>
        /// The actor id of the actor to play the animation on.
        /// </summary>
        public Guid TargetId { get; set; }

        /// <inheritdoc />
        public override void OnTriggered(IMixedRealityExtensionApp app, IUser user, Guid attachedActorId)
        {
            // Play your animation here.
        }
    }
}
