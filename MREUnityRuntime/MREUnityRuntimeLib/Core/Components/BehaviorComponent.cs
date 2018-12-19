// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors;

namespace MixedRealityExtension.Core.Components
{
    internal class BehaviorComponent : ActorComponentBase
    {
        private IBehaviorHandler _behaviorHandler;

        internal void SetBehaviorHandler(IBehaviorHandler behaviorHandler)
        {
            if (_behaviorHandler != null && _behaviorHandler.BehaviorType != behaviorHandler.BehaviorType)
            {
                ClearBehaviorHandler();
            }

            _behaviorHandler = behaviorHandler;
        }

        internal void ClearBehaviorHandler()
        {
            _behaviorHandler.CleanUp();
            _behaviorHandler = null;
        }

        internal bool ContainsBehaviorHandler()
        {
            return _behaviorHandler != null;
        }

        internal override void CleanUp()
        {
            base.CleanUp();
            ClearBehaviorHandler();
        }
    }
}
