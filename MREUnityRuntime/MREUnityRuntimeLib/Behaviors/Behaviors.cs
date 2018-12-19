// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors.Handlers;
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
        [BehaviorHandlerType(typeof(TargetHandler))]
        Target = 1,

        /// <summary>
        /// The button behavior type.
        /// </summary>
        [BehaviorHandlerType(typeof(ButtonHandler))]
        Button = 4,
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal class BehaviorHandlerType : Attribute
    {
        internal Type HandlerType { get; }

        public BehaviorHandlerType(Type behaviorHandlerType)
        {
            HandlerType = behaviorHandlerType;
        }
    }
}
