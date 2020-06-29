// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.Core.Interfaces;

namespace MixedRealityExtension.PluginInterfaces.Behaviors
{
	/// <summary>
	/// This interface serves as the interface to a behavior factory that need to be implemented by the platform utilizing MWI Apps.
	/// </summary>
	public interface IBehaviorFactory
	{
		/// <summary>
		/// Create the concrete behavior that implements the <see cref="ITargetBehavior"/> interface.
		/// </summary>
		/// <param name="actor">The actor that the behavior will be attached to.</param>
		/// /// <param name="context">The target behavior context to operate on.</param>
		/// <returns>The instance of the behavior implementing the <see cref="ITargetBehavior"/> interface.</returns>
		ITargetBehavior GetOrCreateTargetBehavior(IActor actor, TargetBehaviorContext context);

		/// <summary>
		/// Create the concrete behavior that implements the <see cref="IButtonBehavior"/> interface.
		/// </summary>
		/// <param name="actor">The actor that the behavior will be attached to.</param>
		/// <param name="context">The button behavior context to operate on.</param>
		/// <returns>The instance of the behavior implementing the <see cref="IButtonBehavior"/> interface.</returns>
		IButtonBehavior GetOrCreateButtonBehavior(IActor actor, ButtonBehaviorContext context);

		/// <summary>
		/// Create the concrete behavior that implements the <see cref="IPenBehavior"/> interface.
		/// </summary>
		/// <param name="actor">The actor that the behavior will be attached to.</param>
		/// /// <param name="context">The pen behavior context to operate on.</param>
		/// <returns>The instance of the behavior implementing the <see cref="IPenBehavior"/> interface.</returns>
		IPenBehavior GetOrCreatePenBehavior(IActor actor, PenBehaviorContext context);

		// TODO @tombu - This will be added to allow for a more override model for high level behaviors.
		//BehaviorTypeT CreatePrimaryBehaviorOverride<BehaviorTypeT>(PrimaryBehaviorType type, IActor actor);
		//BehaviorTypeT CreateBackgroundBehaviorOverride<BehaviorTypeT>(PrimaryBehaviorType type, IActor actor);
	}
}
