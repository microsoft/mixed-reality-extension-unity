// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace MixedRealityExtension.Behaviors.ActionData
{
	/// <summary>
	/// Abstract class that is the base class for any tool behavior action data.
	/// </summary>
	public abstract class BaseToolData : BaseActionData
	{
		/// <summary>
		/// Gets whether the tool data is empty and shouldn't be synchronized.
		/// </summary>
		public abstract bool IsEmpty { get; }

		/// <summary>
		/// Reset the action data to a initial state.
		/// </summary>
		public abstract void Reset();
	}
}
