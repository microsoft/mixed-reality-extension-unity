// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching.Types;
using System;

namespace MixedRealityExtension.Messaging.Events.Types
{
    internal class AnimationStoppedEvent : MWEventBase
    {
        private string AnimationName;
        private float AnimationTime;

        public AnimationStoppedEvent(Guid actorId, string animationName, float animationTime)
            : base(actorId)
        {
            AnimationName = animationName;
            AnimationTime = animationTime;
        }

        internal override void SendEvent(MixedRealityExtensionApp app)
        {
            app.Protocol.Send(new StopAnimation()
            {
                ActorId = this.ActorId,
                AnimationName = this.AnimationName,
                AnimationTime = this.AnimationTime
            });
        }
    }
}
