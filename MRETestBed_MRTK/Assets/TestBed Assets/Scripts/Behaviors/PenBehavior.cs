// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Assets.Scripts.Tools;
using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.PluginInterfaces.Behaviors;

namespace Assets.Scripts.Behaviors
{
	public class PenBehavior : TargetBehavior, IPenBehavior
	{
		public MWAction<PenData> Holding { get; } = new MWAction<PenData>();

		public MWAction<PenData> Using { get; } = new MWAction<PenData>();

		public override Type GetDesiredToolType()
		{
			return typeof(PenTool);
		}
	}
}
