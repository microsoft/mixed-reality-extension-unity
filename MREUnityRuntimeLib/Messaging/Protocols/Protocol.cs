// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.IPC;
using MixedRealityExtension.Messaging.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MixedRealityExtension.Messaging.Protocols
{
	internal abstract class Protocol : IProtocol
	{
		protected MixedRealityExtensionApp App { get; }

		protected IConnectionInternal Conn => App.Conn;

		public event MWEventHandler OnComplete;
		public event MWEventHandler<Message> OnReceive;

		internal Protocol(MixedRealityExtensionApp app)
		{
			App = app;
		}

		protected abstract void InternalReceive(Message message);

		protected abstract void InternalStart();

		protected abstract void InternalComplete();

		void IProtocol.Receive(Message message)
		{
			InternalReceive(message);
		}

		public void Start()
		{
			if (Conn != null)
			{
				Conn.OnReceive += Conn_OnReceive;
				InternalStart();
			}
		}

		public void Stop()
		{
			if (Conn != null)
			{
				Conn.OnReceive -= Conn_OnReceive;
			}
		}

		private void Conn_OnReceive(Message message)
		{
			try
			{
				if (message.Payload is Payloads.Heartbeat payload)
				{
					App.UpdateServerTimeOffset(payload.ServerTime);
					Send(new Payloads.HeartbeatReply(), message.Id);
				}
				else
				{
					InternalReceive(message);
				}
			}
			catch (Exception ex)
			{
				var errorMessage = $"Failed to process message: {message?.Payload?.Type}\nError: {ex.Message}\nStackTrace: {ex.StackTrace}";
				App.Logger.LogDebug(errorMessage);
				try
				{
					if (message != null && !string.IsNullOrWhiteSpace(message.Id))
					{
						// In case of failure: make a best effort to send a reply message, so promises don't hang and the app can know something about what went wrong.
						Send(new OperationResult()
						{
							Message = errorMessage,
							ResultCode = OperationResultCode.Error
						}, message.Id);
					}
				}
				catch
				{ }
			}
		}

		public void Complete()
		{
			if (Conn != null)
			{
				Conn.OnReceive -= Conn_OnReceive;
				OnComplete?.Invoke();

				foreach (var handler in OnReceive?.GetInvocationList())
				{
					OnReceive -= (MWEventHandler<Message>)handler;
				}
				foreach (var handler in OnComplete?.GetInvocationList())
				{
					OnComplete -= (MWEventHandler)handler;
				}

				InternalComplete();
			}
		}

		public void Send(Message message)
		{
			if (Conn != null)
			{
				message.Id = Guid.NewGuid().ToString();

				try
				{
					Conn.Send(message);
				}
				catch (Exception e)
				{
					// Don't log to App.Logger here. The WebSocket might be disconnected.
					Debug.LogError($"Error sending message. Exception: {e.Message}\nStackTrace: {e.StackTrace}");
				}
			}
		}

		public void Send(Payloads.Payload payload, string replyToId = null)
		{
			var message = new Message()
			{
				ReplyToId = replyToId,
				Payload = payload
			};

			Send(message);
		}

		protected void Dispatch(Message message)
		{
			OnReceive?.Invoke(message);
		}
	}
}
