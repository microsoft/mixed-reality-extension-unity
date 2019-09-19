// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.App;
using MixedRealityExtension.Core.Collision;
using MixedRealityExtension.Messaging.Payloads;
using System;

namespace MixedRealityExtension.Messaging.Events.Types
{
	internal class TriggerEvent: MWEventBase
	{
		private readonly ColliderEventType _eventType;
		private readonly Guid _otherActor;

		internal TriggerEvent(Guid actorId, ColliderEventType eventType, Guid otherActor)
			: base(actorId)
		{
			_eventType = eventType;
			_otherActor = otherActor;
		}

		internal override void SendEvent(MixedRealityExtensionApp app)
		{
			app.Protocol.Send(new TriggerEventRaised()
			{
				ActorId = ActorId,
				EventType = _eventType,
				OtherActorId = _otherActor
			});
		}
	}
}
