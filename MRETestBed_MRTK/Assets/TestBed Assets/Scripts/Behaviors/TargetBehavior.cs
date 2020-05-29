// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Tools;
using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;

namespace Assets.Scripts.Behaviors
{
	public class TargetBehavior : BehaviorBase, ITargetBehavior
	{
		protected TargetBehaviorContext _context;

		public bool Grabbable { get; set; }

		public bool IsGrabbed { get; set; }

		public TargetBehaviorContext Context => _context;

		public override Type GetDesiredToolType()
		{
			return typeof(TargetTool);
		}

		public void SetContext(TargetBehaviorContext context)
		{
			_context = context;
		}
	}
}
