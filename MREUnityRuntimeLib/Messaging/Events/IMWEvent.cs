// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using System;

namespace MixedRealityExtension.Messaging.Events
{
	internal interface IMWEvent
	{
		Guid ActorId { get; }

		void SendEvent(MixedRealityExtensionApp app);
	}
}
