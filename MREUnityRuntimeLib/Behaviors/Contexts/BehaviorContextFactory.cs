// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using System;
using System.Collections.Generic;

using FactoryFunc = System.Func<
	MixedRealityExtension.Core.Interfaces.IActor,
	System.WeakReference<MixedRealityExtension.App.MixedRealityExtensionApp>,
	MixedRealityExtension.Behaviors.Contexts.BehaviorContextBase>;

namespace MixedRealityExtension.Behaviors.Contexts
{
	internal class BehaviorContextFactory
	{
		private static Dictionary<BehaviorType, FactoryFunc> s_factoryMethods =
			new Dictionary<BehaviorType, FactoryFunc>();

		static BehaviorContextFactory()
		{
			s_factoryMethods.Add(BehaviorType.Target, CreateTargetBehaviorContext);
			s_factoryMethods.Add(BehaviorType.Button, CreateButtonBehaviorContext);
			s_factoryMethods.Add(BehaviorType.Pen, CreatePenBehaviorContext);
		}

		internal static BehaviorContextBase CreateContext(BehaviorType behaviorType, IActor actor, WeakReference<MixedRealityExtensionApp> appRef)
		{
			MixedRealityExtensionApp app;
			appRef.TryGetTarget(out app);

			if (MREAPI.AppsAPI.BehaviorFactory == null)
			{
				app?.Logger.LogWarning("Host app does not provide a behavior factory.  Behavior system not available");
				return null;
			}

			if (s_factoryMethods.TryGetValue(behaviorType, out FactoryFunc factoryMethod))
			{
				return factoryMethod.Invoke(actor, appRef);
			}

			app?.Logger.LogError($"Trying to create a behavior of type {behaviorType.ToString()}, but no handler is registered for the given type.");
			return null;
		}

		private static BehaviorContextBase CreateTargetBehaviorContext(IActor actor, WeakReference<MixedRealityExtensionApp> appRef)
		{
			var behaviorFactory = MREAPI.AppsAPI.BehaviorFactory;
			var context = new TargetBehaviorContext();
			var targetBehavior = behaviorFactory.GetOrCreateTargetBehavior(actor, context);
			if (targetBehavior == null) throw new NullReferenceException("Application failed to create a target behavior for the MRE runtime.");
			context.Initialize(targetBehavior, appRef, actor);
			return context;
		}

		private static BehaviorContextBase CreateButtonBehaviorContext(IActor actor, WeakReference<MixedRealityExtensionApp> appRef)
		{
			var behaviorFactory = MREAPI.AppsAPI.BehaviorFactory;
			var context = new ButtonBehaviorContext();
			var buttonBehavior = behaviorFactory.GetOrCreateButtonBehavior(actor, context);
			if (buttonBehavior == null) throw new NullReferenceException("Application failed to create a button behavior for the MRE runtime.");
			context.Initialize(buttonBehavior, appRef, actor);
			return context;
		}

		private static BehaviorContextBase CreatePenBehaviorContext(IActor actor, WeakReference<MixedRealityExtensionApp> appRef)
		{
			var behaviorFactory = MREAPI.AppsAPI.BehaviorFactory;
			var context = new PenBehaviorContext();
			var penBehavior = behaviorFactory.GetOrCreatePenBehavior(actor, context);
			if (penBehavior == null) throw new NullReferenceException("Application failed to create a pen behavior for the MRE runtime.");
			context.Initialize(penBehavior, appRef, actor);
			return context;
		}
	}
}
