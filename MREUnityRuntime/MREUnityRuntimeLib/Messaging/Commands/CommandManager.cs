// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MixedRealityExtension.Messaging.Commands
{
    internal class CommandManager : ICommandManager
    {
        private Dictionary<Type, MethodInfo> _methodHandlers;

        public CommandManager(Type[] commandHandlers)
        {
            // Build table of handler to method info.
            //var methods = commandHandlers[0].GetMethods(BindingFlags.NonPublic | BindingFlags.Instance); //commandHandlers.SelectMany(t => t.GetMethods(BindingFlags.NonPublic));
            //var handlerMethods1 = methods.Where(m => m.GetCustomAttributes().OfType<CommandHandler>().Any());
            //_methodHandlers = handlerMethods1.ToDictionary(m => m.GetCustomAttributes().OfType<CommandHandler>().First().CommandType);

            _methodHandlers = commandHandlers
                .SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
                .Where(m => m.GetCustomAttributes().OfType<CommandHandler>().Any())
                .ToDictionary(m => m.GetCustomAttributes().OfType<CommandHandler>().First().CommandType);
        }

        public void ExecuteCommandPayload(ICommandHandlerContext handlerContext, ICommandPayload commandPayload)
        {
            var commandPayloadType = commandPayload.GetType();
            if (_methodHandlers.ContainsKey(commandPayloadType))
            {
                var commandType = typeof(Command<>);
                Type[] typeArgs = { commandPayloadType };
                commandType = commandType.MakeGenericType(typeArgs);
                var command = (ICommand)Activator.CreateInstance(commandType, new object[] { commandPayload });
                command.Execute(handlerContext, _methodHandlers[commandPayloadType]);
            }
            else
            {
                MREAPI.Logger.LogError($"No command handler registered for command payload: {commandPayloadType.Name}");
            }
        }
    }
}
