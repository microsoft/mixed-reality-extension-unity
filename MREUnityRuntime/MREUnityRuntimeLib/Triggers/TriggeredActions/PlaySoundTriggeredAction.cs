// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.Core.Interfaces;

namespace MixedRealityExtension.Triggers.TriggeredActions
{
    /// <summary>
    /// Class that plays a sound in response to a trigger being fired.
    /// </summary>
    public class PlaySoundTriggeredAction : TriggeredActionBase
    {
        /// <summary>
        /// The id of the sound asset to play
        /// </summary>
        public Guid AssetId { get; set; }

        /// <summary>
        /// The actor id of the actor to play the sound on.
        /// </summary>
        public Guid TargetId { get; set; }

        /// <summary>
        /// The options for playback for the sound.
        /// </summary>
        public MediaStateOptions Options { get; set; }

        /// <inheritdoc />
        public override void OnTriggered(IUser user, Guid attachedActorId)
        {
            // Play your sound here.
        }
    }
}
