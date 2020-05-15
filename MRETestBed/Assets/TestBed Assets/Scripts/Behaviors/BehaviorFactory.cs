// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;

namespace Assets.Scripts.Behaviors
{
	public class BehaviorFactory : IBehaviorFactory
	{
		public IButtonBehavior GetOrCreateButtonBehavior(IActor actor, ButtonBehaviorContext context)
		{
			var buttonBehavior = actor.GameObject.GetComponent<ButtonBehavior>() ?? actor.GameObject.AddComponent<ButtonBehavior>();
			buttonBehavior.Context = context;
			return buttonBehavior;
		}

		public IPenBehavior GetOrCreatePenBehavior(IActor actor, PenBehaviorContext context)
		{
			var penBehavior = actor.GameObject.GetComponent<PenBehavior>() ?? actor.GameObject.AddComponent<PenBehavior>();
			penBehavior.Context = context;
			penBehavior.Grabbable = true;
			return penBehavior;
		}

		public ITargetBehavior GetOrCreateTargetBehavior(IActor actor, TargetBehaviorContext context)
		{
			var targetBehavior = actor.GameObject.GetComponent<TargetBehavior>() ?? actor.GameObject.AddComponent<TargetBehavior>();
			targetBehavior.Context = context;
			return targetBehavior;
		}
	}
}
