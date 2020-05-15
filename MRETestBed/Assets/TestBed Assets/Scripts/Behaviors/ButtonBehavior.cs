// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Tools;
using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;

namespace Assets.Scripts.Behaviors
{
	public class ButtonBehavior : TargetBehavior, IButtonBehavior
	{
		public new ButtonBehaviorContext Context { get; set; }

		public override Type GetDesiredToolType()
		{
			return typeof(ButtonTool);
		}
	}
}
