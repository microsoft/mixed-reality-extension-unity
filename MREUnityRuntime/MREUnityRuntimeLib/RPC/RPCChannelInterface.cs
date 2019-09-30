// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Messaging.Payloads;
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.RPC
{
	public sealed class RPCChannelInterface
	{
		private Dictionary<string, RPCInterface> channelHandlers = new Dictionary<string, RPCInterface>();
		private RPCInterface globalHandler;

		public void SetChannelHandler(string channelName, RPCInterface handler)
		{
			if (!string.IsNullOrEmpty(channelName))
			{
				if (handler != null)
				{
					channelHandlers.Add(channelName, handler);
				}
				else
				{
					channelHandlers.Remove(channelName);
				}
			}
			else
			{
				globalHandler = handler;
			}
		}

		internal void ReceiveRPC(AppToEngineRPC payload)
		{
			RPCInterface handler;
			if (!string.IsNullOrEmpty(payload.ChannelName))
			{
				channelHandlers.TryGetValue(payload.ChannelName, out handler);
			}
			else
			{
				handler = globalHandler;
			}
			if (handler != null)
			{
				handler.ReceiveRPC(payload);
			}
		}
	}
}
