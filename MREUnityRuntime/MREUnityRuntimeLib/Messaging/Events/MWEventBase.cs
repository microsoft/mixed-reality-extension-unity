// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using System;

namespace MixedRealityExtension.Messaging.Events
{
	internal abstract class MWEventBase : IMWEvent
	{
		internal Guid ActorId { get; }

		internal MWEventBase(Guid actorId)
		{
			ActorId = actorId;
		}

		internal abstract void SendEvent(MixedRealityExtensionApp app);

		#region IMWEvent

		Guid IMWEvent.ActorId => this.ActorId;

		void IMWEvent.SendEvent(MixedRealityExtensionApp app)
		{
			this.SendEvent(app);
		}

		#endregion
	}
}
