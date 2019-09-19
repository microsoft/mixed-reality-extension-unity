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
		/// <summary>
		/// Ensures a command handler's onCompleteCallback is called exactly once and in a timely manner.
		/// </summary>
		private class PendingCompletionCallback
		{
			public bool Invoked;
			public string Location;
			public DateTime CreationTime;
		}

		// Command handler methods mapped to MethodInfo by handler object type.
		private readonly Dictionary<Type, Dictionary<Type, MethodInfo>> _methodHandlersByContextType = new Dictionary<Type, Dictionary<Type, MethodInfo>>();
		// Registered invocation targets.
		private readonly Dictionary<Type, ICommandHandlerContext> _invocationTargets = new Dictionary<Type, ICommandHandlerContext>();
		// The list of running commands that haven't yet invoked their onCompleteCallback handler.
		private readonly List<PendingCompletionCallback> _pendingCompletionCallbacks = new List<PendingCompletionCallback>();
		// The next time to check the _pendingCompletionCallbacks queue for timed out commands.
		// These commands may have a logic error and are not calling their supplied onCompleteCallback.
		private DateTime _nextQueueCheckTime;

		// The maximum amount of time to wait for an onCompleteCallback to be invoked.
		private static readonly TimeSpan QueuedCompletionCallbackTimeout = TimeSpan.FromSeconds(60);

		public CommandManager(Dictionary<Type, ICommandHandlerContext> commandHandlers)
		{
			foreach (var commandHandlerPair in commandHandlers)
			{
				// Build table of handler to method info.
				_methodHandlersByContextType.Add(commandHandlerPair.Key,
					new Type[] { commandHandlerPair.Key }
						.SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
						.Where(m => m.GetCustomAttributes().OfType<CommandHandler>().Any())
						.ToDictionary(m => m.GetCustomAttributes().OfType<CommandHandler>().First().CommandType));

				_invocationTargets.Add(commandHandlerPair.Key, commandHandlerPair.Value);
			}
		}

		public void ExecuteCommandPayload(ICommandPayload commandPayload, Action onCompleteCallback)
		{
			var commandPayloadType = commandPayload.GetType();
			foreach (var methodHandlersPair in _methodHandlersByContextType)
			{
				if (!methodHandlersPair.Value.ContainsKey(commandPayloadType))
				{
					continue;
				}
				if (!_invocationTargets.TryGetValue(methodHandlersPair.Key, out ICommandHandlerContext handlerContext) || handlerContext == null)
				{
					continue;
				}
				ExecuteCommandPayload(handlerContext, commandPayload, methodHandlersPair.Value[commandPayloadType], onCompleteCallback);
				return;
			}
			throw new Exception($"No command handler for command payload: {commandPayloadType.Name}.");
		}

		public void ExecuteCommandPayload(ICommandHandlerContext handlerContext, ICommandPayload commandPayload, Action onCompleteCallback)
		{
			var handlerContextType = handlerContext.GetType();
			if (_methodHandlersByContextType.TryGetValue(handlerContextType, out Dictionary<Type, MethodInfo> methodHandlers))
			{
				var commandPayloadType = commandPayload.GetType();
				if (methodHandlers.ContainsKey(commandPayloadType))
				{
					ExecuteCommandPayload(handlerContext, commandPayload, methodHandlers[commandPayloadType], onCompleteCallback);
				}
				else
				{
					throw new Exception($"No command handler for command payload: {commandPayloadType.Name} on type: {handlerContextType.Name}");
				}
			}
			else
			{
				throw new Exception($"No command handlers registered for type: {handlerContextType.Name}");
			}
		}

		private void ExecuteCommandPayload(ICommandHandlerContext handlerContext, ICommandPayload commandPayload, MethodInfo methodHandler, Action onCompleteCallback)
		{
			var handlerContextType = handlerContext.GetType();
			var commandPayloadType = commandPayload.GetType();
			var commandType = typeof(Command<>);
			Type[] typeArgs = { commandPayloadType };
			commandType = commandType.MakeGenericType(typeArgs);
			var command = (ICommand)Activator.CreateInstance(
				commandType,
				new object[] {
							commandPayload,
							CreateQueuedCompletionCallback(
								$"{handlerContextType.Name}.{methodHandler.Name}",
								onCompleteCallback
							) });
			command.Execute(handlerContext, methodHandler);
		}

		public void Update()
		{
			// Periodically check for timed out completion callback invocations.
			if (_pendingCompletionCallbacks.Count > 0)
			{
				var currTime = DateTime.Now;
				if (_nextQueueCheckTime <= currTime)
				{
					while (_pendingCompletionCallbacks.Count > 0)
					{
						// Callbacks are naturally sorted by creation time.
						var callback = _pendingCompletionCallbacks.First();
						if (callback.CreationTime <= currTime + QueuedCompletionCallbackTimeout)
						{
							// Command handler did not invoke its onCompleteCallback in a timely manner.
							MREAPI.Logger.LogError($"ERROR: Timeout waiting for onCompleteCallback invocation from {callback.Location}");
							_pendingCompletionCallbacks.Remove(callback);

							// TODO: Report this error up to the app, once we have implemented error logging payloads.
							// Task: https://github.com/Microsoft/mixed-reality-extension-sdk/issues/24
						}
						else
						{
							break;
						}
					}

					// If there are still items in the list, schedule a time to check it again in the near future.
					if (_pendingCompletionCallbacks.Count > 0)
					{
						_nextQueueCheckTime = currTime + TimeSpan.FromSeconds(5);
					}
				}
			}
		}

		private Action CreateQueuedCompletionCallback(string location, Action onCompleteCallback)
		{
			if (onCompleteCallback == null)
			{
				return null;
			}

			// If the queue was previously empty then schedule a time to check it later.
			if (_pendingCompletionCallbacks.Count == 0)
			{
				_nextQueueCheckTime = DateTime.Now + QueuedCompletionCallbackTimeout + TimeSpan.FromSeconds(1);
			}

			var callback = new PendingCompletionCallback()
			{
				Invoked = false,
				Location = location,
				CreationTime = DateTime.Now
			};

			_pendingCompletionCallbacks.Add(callback);

			return () =>
			{
				if (!callback.Invoked)
				{
					// Mark as invoked, remove from queue, and make the actual callback.
					callback.Invoked = true;
					_pendingCompletionCallbacks.Remove(callback);
					onCompleteCallback.Invoke();
				}
				else
				{
					// If the callback is called multiple times that's an error.
					MREAPI.Logger.LogError($"ERROR: onCompleteCallback invoked more than once from {callback.Location}.");
				}
			};
		}
	}
}
