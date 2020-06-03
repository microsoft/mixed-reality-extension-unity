// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;

namespace Assets.Scripts.Behaviors
{
	public class MRTKBehaviorFactory : IBehaviorFactory
	{
		public IButtonBehavior GetOrCreateButtonBehavior(IActor actor, ButtonBehaviorContext context)
		{
			var buttonBehavior = actor.GameObject.GetComponent<MRTKButtonBehavior>() ?? actor.GameObject.AddComponent<MRTKButtonBehavior>();
			buttonBehavior.SetContext(context);
			return buttonBehavior;
		}

		public IPenBehavior GetOrCreatePenBehavior(IActor actor, PenBehaviorContext context)
		{
			throw new System.NotImplementedException();
		}

		public ITargetBehavior GetOrCreateTargetBehavior(IActor actor, TargetBehaviorContext context)
		{
			var targetBehavior = actor.GameObject.GetComponent<MRTKTargetBehavior>() ?? actor.GameObject.AddComponent<MRTKTargetBehavior>();
			targetBehavior.SetContext(context);
			return targetBehavior;
		}
	}
}
