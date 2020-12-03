// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using MixedRealityExtension.App;
using MixedRealityExtension.Messaging.Payloads;

namespace MixedRealityExtension.RPC
{
	/// <summary>
	/// Class that represents the remote procedure call interface for the MRE interop library.
	/// </summary>
	public sealed class RPCInterface
	{
		private readonly IMixedRealityExtensionApp _app;
		private MixedRealityExtensionApp app => (MixedRealityExtensionApp)_app;
		private Dictionary<string, RPCHandlerBase> _handlers = new Dictionary<string, RPCHandlerBase>();

		public RPCInterface(IMixedRealityExtensionApp app) => _app = app;

		/// <summary>
		/// Registers and RPC handler for the specific procedure name
		/// </summary>
		/// <param name="procName">The name of the remote procedure.</param>
		/// <param name="handler">The handler to be called when an RPC call is received for the given procedure name.</param>
		public void OnReceive(string procName, RPCHandlerBase handler)
		{
			_handlers[procName] = handler;
		}

		internal void ReceiveRPC(AppToEngineRPC payload)
		{
			if (_handlers.ContainsKey(payload.ProcName))
			{
				// Filter the message by userId, if present.
				// Message is also filtered on the server, so
				// this is just extra protection.
				if (!string.IsNullOrEmpty(payload.UserId))
				{
					Guid userId = new Guid(payload.UserId);
					if (app.FindUser(userId) == null)
					{
						return;
					}
				}

				try
				{
					_handlers[payload.ProcName].Execute(payload.Args.Children().ToArray());
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogError(e);
				}
			}
		}

		/// <summary>
		/// Sends an RPC message to the app with the given name and arguments.
		/// </summary>
		/// <param name="procName">The name of the remote procedure call.</param>
		/// <param name="args">The arguments for the remote procedure call.</param>
		public void SendRPC(string procName, params object[] args)
		{
			app.Protocol.Send(new EngineToAppRPC()
			{
				ProcName = procName,
				Args = (new List<object>(args)).ToArray()
			});
		}

		/// <summary>
		/// Sends an RPC message to the app with the given name and arguments.
		/// </summary>
		/// <param name="channelName">The name of the channel of this remote procedure call.</param>
		/// <param name="procName">The name of the remote procedure call.</param>
		/// <param name="userId">The id of the user this rpc call is targeting.</param>
		/// <param name="args">The arguments for the remote procedure call.</param>
		public void SendRPC(string channelName, string procName, string userId, params object[] args)
		{
			app.Protocol.Send(new EngineToAppRPC()
			{
				ChannelName = channelName,
				ProcName = procName,
				UserId = userId,
				Args = (new List<object>(args)).ToArray()
			});
		}
	}
}
