// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using System;

namespace MixedRealityExtension.Triggers.TriggeredActions
{
    /// <summary>
    /// Interface that represents an action that can be trigged by a trigger within the MRE runtime.
    /// </summary>
    public interface ITriggeredAction
    {
        /// <summary>
        /// Method that is called when a trigger is fired.
        /// </summary>
        /// <param name="user">The user that caused the trigger to fire.</param>
        /// <param name="attachedActorId">The actor that the trigger was attached to.</param>
        void OnTriggered(IMixedRealityExtensionApp app, IUser user, Guid attachedActorId);
    }
}
