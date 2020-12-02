// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedRealityExtension.Messaging.Payloads
{
	public class Payload
	{
		public string Type { get; private set; }

		public IList<Trace> Traces { get; set; }

		public Payload()
		{
			Type = PayloadTypeRegistry.GetNetworkType(this.GetType());

#if ANDROID_DEBUG
			MREAPI.Logger.LogDebug($"Creating payload of type {Type} for the payload class type {this.GetType()}");
#endif
		}

		public void AddTrace(Trace trace)
		{
			Traces = Traces ?? new List<Trace>();
			Traces.Add(trace);
		}
	}

	public class NetworkCommandPayload : Payload, INetworkCommandPayload
	{
		public string MessageId { get; set; }
	}
}
