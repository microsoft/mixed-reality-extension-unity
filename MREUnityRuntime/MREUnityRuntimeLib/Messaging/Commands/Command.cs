// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Reflection;

namespace MixedRealityExtension.Messaging.Commands
{
    internal class Command<T> : ICommand where T : ICommandPayload
    {
        private readonly T _commandPayload;

        public Command(T commandPayload)
        {
            _commandPayload = commandPayload;
        }

        public void Execute(ICommandHandlerContext handlerContext, MethodInfo handlerMethod)
        {
            if (handlerMethod != null)
            {
                handlerMethod.Invoke(handlerContext, new object[] { _commandPayload });
            }
        }
    }
}
