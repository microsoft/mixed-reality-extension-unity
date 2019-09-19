// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Messaging.Payloads;
using System;

namespace MixedRealityExtension.Messaging.Events.Types
{
	internal class UserEvent : MWEventBase
	{
		private Payload _payload;

		internal UserEvent(Guid userId, Payload payload)
			: base(userId)
		{
			_payload = payload;
		}

		internal override void SendEvent(MixedRealityExtensionApp app)
		{
			app.Protocol.Send(_payload);
		}
	}
}
