// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.Core.Interfaces;

namespace MixedRealityExtension.Triggers.TriggeredActions
{
    /// <summary>
    /// Base class that all triggered actions are based on.
    /// </summary>
    public abstract class TriggeredActionBase : ITriggeredAction
    {
        /// <summary>
        /// Property that is the string representation of the triggered action type.
        /// </summary>
        public string Type { get; set; }

        /// <inheritdoc />
        public abstract void OnTriggered(IUser user, Guid attachedActorId);
    }
}
