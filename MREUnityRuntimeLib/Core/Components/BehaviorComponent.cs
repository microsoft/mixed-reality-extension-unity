// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors;
using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.PluginInterfaces.Behaviors;

namespace MixedRealityExtension.Core.Components
{
	internal class BehaviorComponent : ActorComponentBase
	{
		private BehaviorContextBase _behaviorContext;

		internal IBehavior Behavior => _behaviorContext?.Behavior;

		internal BehaviorContextBase Context => _behaviorContext;

		internal void SetBehaviorContext(BehaviorContextBase behaviorContext)
		{
			if (_behaviorContext != null && _behaviorContext.BehaviorType != behaviorContext.BehaviorType)
			{
				ClearBehaviorContext();
			}

			_behaviorContext = behaviorContext;
		}

		internal void ClearBehaviorContext()
		{
			if (_behaviorContext != null)
			{
				_behaviorContext.CleanUp();
				_behaviorContext = null;
			}
		}

		internal bool ContainsBehaviorContext()
		{
			return _behaviorContext != null;
		}

		internal override void CleanUp()
		{
			base.CleanUp();
			ClearBehaviorContext();
		}

		internal override void SynchronizeComponent()
		{
			_behaviorContext?.SynchronizeBehavior();
		}

		private void FixedUpdate()
		{
			_behaviorContext?.FixedUpdate();
		}
	}
}
