// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.App;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching.Types;

namespace MixedRealityExtension.Messaging.Events.Types
{
	internal class PhysicsBridgeUpdated : MWEventBase
	{
		private readonly PhysicsBridgePatch _physicsBridgePatch;

		public PhysicsBridgeUpdated(Guid id, PhysicsBridgePatch physicsBridgePatch) 
			: base(id)
		{
			_physicsBridgePatch = physicsBridgePatch;
		}

		internal override void SendEvent(MixedRealityExtensionApp app)
		{
			if (app.Protocol == null)
			{
				return;
			}

			app.Protocol.Send(new PhysicsBridgeUpdate()
			{
				PhysicsBridgePatch = _physicsBridgePatch
			});
		}
	}

	internal class PhysicsTranformServerUploadUpdated : MWEventBase
	{
		private readonly PhysicsTranformServerUploadPatch _physicsTransformUploadPatch;

		public PhysicsTranformServerUploadUpdated(Guid id, PhysicsTranformServerUploadPatch physicsServerUploadPatch)
			: base(id)
		{
			_physicsTransformUploadPatch = physicsServerUploadPatch;
		}

		internal override void SendEvent(MixedRealityExtensionApp app)
		{
			if (app.Protocol == null)
			{
				return;
			}

			app.Protocol.Send(new PhysicsTranformServerUpload()
			{
				PhysicsTranformServer = _physicsTransformUploadPatch
			});
		}
	}

}
