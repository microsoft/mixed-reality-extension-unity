// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.Core.Interfaces;

namespace MixedRealityExtension.Behaviors.Handlers.ActionStateHandlers
{
    public class PlayAnimationActionHandler : IActionStateHandler
    {
        public string AnimationName { get; set; }

        public Guid AnimationTargetId { get; set; }

        public void OnActionStateTriggered(IUser user, Guid attachedActorId)
        {
            // Play your animation here.
        }
    }
}
