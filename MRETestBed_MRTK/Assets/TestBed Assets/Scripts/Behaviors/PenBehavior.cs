// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Assets.Scripts.Tools;
using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.PluginInterfaces.Behaviors;

namespace Assets.Scripts.Behaviors
{
	public class PenBehavior : TargetBehavior, IPenBehavior
	{
		public new PenBehaviorContext Context => _context as PenBehaviorContext;

		public override Type GetDesiredToolType()
		{
			return typeof(PenTool);
		}
	}
}
