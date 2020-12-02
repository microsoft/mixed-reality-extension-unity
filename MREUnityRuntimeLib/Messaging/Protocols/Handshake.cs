// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.IPC;
using MixedRealityExtension.Messaging.Payloads;

namespace MixedRealityExtension.Messaging.Protocols
{
	internal class Handshake : Protocol
	{
		public event MWEventHandler<OperatingModel> OnOperatingModel;

		internal Handshake(MixedRealityExtensionApp app)
			: base(app)
		{
		}

		protected override void InternalStart()
		{
			var handshake = new Payloads.Handshake()
			{
			};

			Send(handshake);
		}

		protected override void InternalComplete()
		{
			foreach (var handler in OnOperatingModel?.GetInvocationList())
			{
				OnOperatingModel -= (MWEventHandler<OperatingModel>)handler;
			}
		}

		protected override void InternalReceive(Message message)
		{
			if (message.Payload is HandshakeReply handshakeReply)
			{
				OnOperatingModel?.Invoke(handshakeReply.OperatingModel);

				Send(new HandshakeComplete());

				Complete();
			}
			else
			{
				App.Logger.LogDebug("Unexpected message");
			}
		}
	}
}
