// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.IPC;

// TODO: Objects should not be visible until synchronization is complete.

namespace MixedRealityExtension.Messaging.Protocols
{
	internal class Idle : Protocol
	{
		internal Idle(MixedRealityExtensionApp app)
		   : base(app)
		{ }

		protected override void InternalStart()
		{
		}

		protected override void InternalComplete()
		{
		}

		protected override void InternalReceive(Message message)
		{
		}
	}
}
