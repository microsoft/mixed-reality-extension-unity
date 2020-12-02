// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using MixedRealityExtension.Messaging.Payloads;

namespace MixedRealityExtension.RPC
{
	public sealed class RPCChannelInterface
	{
		private readonly Dictionary<string, RPCInterface> _channelHandlers = new Dictionary<string, RPCInterface>();
		private RPCInterface _globalHandler;

		public void SetChannelHandler(string channelName, RPCInterface handler)
		{
			if (!string.IsNullOrEmpty(channelName))
			{
				if (handler != null)
				{
					_channelHandlers.Add(channelName, handler);
				}
				else
				{
					_channelHandlers.Remove(channelName);
				}
			}
			else
			{
				_globalHandler = handler;
			}
		}

		internal void ReceiveRPC(AppToEngineRPC payload)
		{
			RPCInterface handler;
			if (!string.IsNullOrEmpty(payload.ChannelName))
			{
				_channelHandlers.TryGetValue(payload.ChannelName, out handler);
			}
			else
			{
				handler = _globalHandler;
			}

			handler?.ReceiveRPC(payload);
		}
	}
}
