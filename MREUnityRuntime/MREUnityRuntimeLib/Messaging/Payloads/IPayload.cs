// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Messaging.Commands;

namespace MixedRealityExtension.Messaging.Payloads
{
	public interface IPayload
	{
		string Type { get; }
	}

	public interface INetworkCommandPayload : IPayload, ICommandPayload
	{
		string MessageId { get; set; }
	}
}
