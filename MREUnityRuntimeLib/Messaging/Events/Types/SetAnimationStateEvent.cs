// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.App;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching.Types;
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Messaging.Events.Types
{
	internal class SetAnimationStateEvent : MWEventBase
	{
		private readonly string animationName;
		private readonly float? animationTime;
		private readonly float? animationSpeed;
		private readonly bool? animationEnabled;

		public SetAnimationStateEvent(
			Guid actorId,
			string animationName,
			float? animationTime,
			float? animationSpeed,
			bool? animationEnabled)
			: base(actorId)
		{
			this.animationName = animationName;
			this.animationTime = animationTime;
			this.animationSpeed = animationSpeed;
			this.animationEnabled = animationEnabled;
		}

		internal override void SendEvent(MixedRealityExtensionApp app)
		{
			app.Protocol.Send(new SetAnimationState
			{
				ActorId = this.ActorId,
				AnimationName = this.animationName,
				State = new MWSetAnimationStateOptions
				{
					Time = this.animationTime,
					Speed = this.animationSpeed,
					Enabled = this.animationEnabled
				}
			});
		}
	}
}
