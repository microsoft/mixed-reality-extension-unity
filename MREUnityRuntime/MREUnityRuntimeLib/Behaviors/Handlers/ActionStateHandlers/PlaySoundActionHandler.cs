// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.Core.Interfaces;

namespace MixedRealityExtension.Behaviors.Handlers.ActionStateHandlers
{
    public class PlaySoundActionHandler : IActionStateHandler
    {
        public string SoundName { get; set; }

        public void OnActionStateTriggered(IUser user, Guid attachedActorId)
        {
            // Play your sound here.
        }
    }
}
