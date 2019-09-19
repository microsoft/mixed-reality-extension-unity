// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Messaging.Commands
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	internal class CommandHandler : Attribute
	{
		public Type CommandType { get; }

		public CommandHandler(Type commandType)
		{
			CommandType = commandType;
		}
	}
}
