// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Messaging.Payloads;

namespace MixedRealityExtension.Messaging
{
	/// <summary>
	/// Represents a message sent and received over a connection.
	/// </summary>
	public class Message
	{
		/// <summary>
		/// The message unique id.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// If this message is a reply, this is the id of the message being replied to.
		/// </summary>
		public string ReplyToId { get; set; }

		/// <summary>
		/// The message payload.
		/// </summary>
		public Payload Payload { get; set; }
	}
}
