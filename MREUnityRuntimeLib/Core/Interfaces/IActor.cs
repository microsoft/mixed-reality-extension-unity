// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Types;

namespace MixedRealityExtension.Core.Interfaces
{
	/// <summary>
	/// The interface that represents an actor within the mixed reality extension runtime.
	/// </summary>
	public interface IActor : IMixedRealityExtensionObject
	{
		/// <summary>
		/// Gets the ID of the actor's parent.
		/// </summary>
		IActor Parent { get; }

		/// <summary>
		/// Gets and sets the name of the actor.
		/// </summary>
		new string Name { get; set; }

		/// <summary>
		/// Gets the app that the actor is owned by.
		/// </summary>
		IMixedRealityExtensionApp App { get; }

		/// <summary>
		/// Gets the local space transform of the actor.
		/// </summary>
		MWScaledTransform LocalTransform { get; }

		/// <summary>
		/// The app space transform of this mixed reality extension object.
		/// </summary>
		MWTransform AppTransform { get; }
	}
}
