// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Messaging.Payloads;
using System;

namespace MixedRealityExtension.Messaging.Events.Types
{
	class ActorCorrectionEvent: MWEventBase
	{
		private readonly ActorCorrection _actorCorrection;

		internal ActorCorrectionEvent(Guid actorId, ActorCorrection actorCorrection)
			: base(actorId)
		{
			_actorCorrection = actorCorrection;
		}

		internal override void SendEvent(MixedRealityExtensionApp app)
		{
			app.Protocol.Send(_actorCorrection);
		}
	}
}
