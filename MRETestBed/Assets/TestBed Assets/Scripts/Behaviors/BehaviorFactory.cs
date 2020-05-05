// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;

namespace Assets.Scripts.Behaviors
{
	public class BehaviorFactory : IBehaviorFactory
	{
		public IButtonBehavior GetOrCreateButtonBehavior(IActor actor)
		{
			return actor.GameObject.GetComponent<ButtonBehavior>() ?? actor.GameObject.AddComponent<ButtonBehavior>();
		}

		public IPenBehavior GetOrCreatePenBehavior(IActor actor)
		{
			var penBehavior = actor.GameObject.GetComponent<PenBehavior>() ?? actor.GameObject.AddComponent<PenBehavior>();
			penBehavior.Grabbable = true;
			return penBehavior;
		}

		public ITargetBehavior GetOrCreateTargetBehavior(IActor actor)
		{
			return actor.GameObject.GetComponent<TargetBehavior>() ?? actor.GameObject.AddComponent<TargetBehavior>();
		}
	}
}
