// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Tools;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;

namespace Assets.Scripts.Behaviors
{
	public class TargetBehavior : BehaviorBase, ITargetBehavior
	{
		public bool Grabbable { get; set; }

		public bool IsGrabbed { get; set; }

		public MWAction Target { get; } = new MWAction();

		public MWAction Grab { get; } = new MWAction();

		public override Type GetDesiredToolType()
		{
			return typeof(TargetTool);
		}
	}
}
