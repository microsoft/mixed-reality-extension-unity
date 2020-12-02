// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Reflection;

namespace MixedRealityExtension.Messaging.Commands
{
	internal interface ICommand
	{
		void Execute(ICommandHandlerContext handlerContext, MethodInfo handlerMethod);
	}
}
