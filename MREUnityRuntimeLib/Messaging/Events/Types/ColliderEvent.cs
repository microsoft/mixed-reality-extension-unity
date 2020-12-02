// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Collision;
using MixedRealityExtension.Messaging.Payloads;

namespace MixedRealityExtension.Messaging.Events.Types
{
	internal class CollisionEvent: MWEventBase
	{
		private readonly ColliderEventType _eventType;
		private readonly CollisionData _collisionData;

		internal CollisionEvent(Guid actorId, ColliderEventType eventType, CollisionData collisionData)
			: base(actorId)
		{
			_eventType = eventType;
			_collisionData = collisionData;
		}

		internal override void SendEvent(MixedRealityExtensionApp app)
		{
			app.Protocol.Send(new CollisionEventRaised()
			{
				ActorId = ActorId,
				EventType = _eventType,
				CollisionData = _collisionData
			});
		}
	}
}
