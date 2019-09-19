// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;

namespace MixedRealityExtension.Behaviors.Handlers
{
	internal class ButtonHandler : TargetHandler
	{
		internal ButtonHandler(IButtonBehavior button, WeakReference<MixedRealityExtensionApp> appRef, IActor attachedActor)
			: base(button, appRef, attachedActor)
		{
			RegisterActionHandler(button.Hover, nameof(button.Hover));
			RegisterActionHandler(button.Click, nameof(button.Click));
			RegisterActionHandler(button.Button, nameof(button.Button));
		}

		internal static new ButtonHandler Create(IActor actor, WeakReference<MixedRealityExtensionApp> appRef)
		{
			var behaviorFactory = MREAPI.AppsAPI.BehaviorFactory;
			var buttonBehavior = behaviorFactory.GetOrCreateButtonBehavior(actor);
			return new ButtonHandler(buttonBehavior, appRef, actor);
		}
	}
}
