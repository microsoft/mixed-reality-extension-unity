// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching.Types;
using System;

namespace MixedRealityExtension.Messaging.Events.Types
{
	internal class ActorChangedEvent : MWEventBase
	{
		private readonly ActorPatch _actor;

		internal ActorChangedEvent(Guid actorId, ActorPatch actor)
			: base(actorId)
		{
			_actor = actor;
		}

		internal override void SendEvent(MixedRealityExtensionApp app)
		{
			app.Protocol.Send(new ActorUpdate()
			{
				Actor = _actor
			});
		}
	}
}
