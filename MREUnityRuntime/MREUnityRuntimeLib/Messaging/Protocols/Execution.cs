// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.App;

namespace MixedRealityExtension.Messaging.Protocols
{
	internal class Execution : Protocol
	{
		internal Execution(MixedRealityExtensionApp app)
			: base(app)
		{ }

		protected override void InternalStart()
		{
		}

		protected override void InternalComplete()
		{
		}

		protected override void InternalReceive(Message message)
		{
			Dispatch(message);
		}
	}
}
