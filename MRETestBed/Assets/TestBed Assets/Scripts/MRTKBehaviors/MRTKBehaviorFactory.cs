// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;

namespace Assets.Scripts.Behaviors
{
	public class MRTKBehaviorFactory : IBehaviorFactory
	{
		public IButtonBehavior GetOrCreateButtonBehavior(IActor actor)
		{
			return actor.GameObject.GetComponent<MRTKButtonBehavior>() ?? actor.GameObject.AddComponent<MRTKButtonBehavior>();
		}

		public ITargetBehavior GetOrCreateTargetBehavior(IActor actor)
		{
			return actor.GameObject.GetComponent<MRTKTargetBehavior>() ?? actor.GameObject.AddComponent<MRTKTargetBehavior>();
		}
	}
}
