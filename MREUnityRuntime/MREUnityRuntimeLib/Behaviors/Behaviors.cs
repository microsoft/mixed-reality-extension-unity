// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors.Contexts;
using System;

namespace MixedRealityExtension.Behaviors
{
	/// <summary>
	/// The type of behavior as a flag supported enum value.
	/// </summary>
	[Flags]
	public enum BehaviorType
	{
		/// <summary>
		/// None behavior.
		/// </summary>
		None = 0,

		/// <summary>
		/// The target behavior type.
		/// </summary>
		[BehaviorContextType(typeof(TargetBehaviorContext))]
		Target = 1,

		/// <summary>
		/// The button behavior type.
		/// </summary>
		[BehaviorContextType(typeof(ButtonBehaviorContext))]
		Button = 2,

		/// <summary>
		/// The pen behavior type.
		/// </summary>
		[BehaviorContextType(typeof(PenBehaviorContext))]
		Pen = 4
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	internal class BehaviorContextType : Attribute
	{
		internal Type ContextType { get; }

		public BehaviorContextType(Type behaviorContextType)
		{
			ContextType = behaviorContextType;
		}
	}
}
