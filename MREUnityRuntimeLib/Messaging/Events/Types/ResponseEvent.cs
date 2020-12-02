// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.App;
using MixedRealityExtension.Messaging.Payloads;

namespace MixedRealityExtension.Messaging.Events.Types
{
	internal class ResponseEvent : MWEventBase
	{
		private string _responseId;
		private Payload _payload;

		internal ResponseEvent(Guid actorId, string responseId, Payload payload)
			: base(actorId)
		{
			_responseId = responseId;
			_payload = payload;
		}

		internal override void SendEvent(MixedRealityExtensionApp app)
		{
			app.Protocol.Send(_payload, _responseId);
		}
	}
}
