// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.IPC;
using MixedRealityExtension.Messaging.Payloads;

namespace MixedRealityExtension.Messaging.Protocols
{
	internal interface IProtocol
	{
		event MWEventHandler OnComplete;

		void Start();

		void Stop();

		void Complete();

		void Receive(Message message);

		void Send(Message message);

		void Send(Payload payload, string replyToId = null);
	}
}
