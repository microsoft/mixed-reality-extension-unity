// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;

// TODO: Objects should not be visible until synchronization is complete.

namespace MixedRealityExtension.Messaging.Protocols
{
	internal class Sync : Protocol
	{
		internal Sync(MixedRealityExtensionApp app)
		   : base(app)
		{ }

		protected override void InternalStart()
		{
			Send(new Payloads.SyncRequest());
		}

		protected override void InternalComplete()
		{
		}

		protected override void InternalReceive(Message message)
		{
			if (message.Payload is Payloads.SyncComplete)
			{
				Complete();
			}
			else
			{
				Dispatch(message);
			}
		}
	}
}
