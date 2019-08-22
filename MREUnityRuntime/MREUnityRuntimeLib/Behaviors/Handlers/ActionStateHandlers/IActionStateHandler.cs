// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using System;

namespace MixedRealityExtension.Behaviors.Handlers.ActionStateHandlers
{
    public interface IActionStateHandler
    {
        void OnActionStateTriggered(IUser user, Guid attachedActorId);
    }
}
