// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Messaging.Payloads;
using System;

namespace MixedRealityExtension.Messaging.Events.Types
{
	internal class BehaviorEvent : MWEventBase
	{
		private readonly ActionPerformed _actionPerformed;

		public BehaviorEvent(ActionPerformed actionPerformed)
			: base(actionPerformed.UserId)
		{
			_actionPerformed = actionPerformed;
		}

		internal override void SendEvent(MixedRealityExtensionApp app)
		{
			app.Protocol.Send(_actionPerformed);
		}
	}
}
