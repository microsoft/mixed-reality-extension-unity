// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Reflection;

namespace MixedRealityExtension.Messaging.Commands
{
	internal class Command<T> : ICommand where T : ICommandPayload
	{
		private readonly T _commandPayload;
		private Action _onCompleteCallback;

		public Command(T commandPayload, Action onCompleteCallback)
		{
			_commandPayload = commandPayload;
			_onCompleteCallback = onCompleteCallback;
		}

		public void Execute(ICommandHandlerContext handlerContext, MethodInfo handlerMethod)
		{
			if (handlerMethod != null)
			{
				handlerMethod.Invoke(handlerContext, new object[] { _commandPayload, _onCompleteCallback });
			}
		}
	}
}
