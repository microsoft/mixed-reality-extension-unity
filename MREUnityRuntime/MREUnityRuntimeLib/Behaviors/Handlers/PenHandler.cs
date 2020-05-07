// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;

namespace MixedRealityExtension.Behaviors.Handlers
{
	internal class PenHandler : ToolHandler<PenData>
	{
		internal PenHandler(IPenBehavior tool, WeakReference<MixedRealityExtensionApp> appRef, IActor attachedActor)
			: base(tool, appRef, attachedActor)
		{
			
		}

		internal static PenHandler Create(IActor actor, WeakReference<MixedRealityExtensionApp> appRef)
		{
			var behaviorFactory = MREAPI.AppsAPI.BehaviorFactory;
			var penBehavior = behaviorFactory.GetOrCreatePenBehavior(actor);
			if (penBehavior == null) throw new NullReferenceException("Application failed to create a pen behavior for the MRE runtime.");
			return new PenHandler(penBehavior, appRef, actor);
		}

		protected override void FixedUpdate()
		{
			base.FixedUpdate();
			if (IsUsing)
			{
				ToolData.DrawData.Add(new DrawData() { Transform = Behavior.Actor.AppTransform });
			}
		}
	}
}
