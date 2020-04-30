// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.IPC;
using MixedRealityExtension.Messaging.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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

		private void Conn_OnReceive(string json)
		{
			try
			{
				if (MREAPI.AppsAPI.VerboseLogging)
				{
					App.Logger.LogDebug($"Recv: {json}");
				}

				var message = JsonConvert.DeserializeObject<Message>(json, Constants.SerializerSettings);

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
				var message = $"Failed to process message: {json}\nError: {ex.Message}\nStackTrace: {ex.StackTrace}";
				App.Logger.LogDebug(message);
				try
				{
					// In case of failure: make a best effort to send a reply message, so promises don't hang and the app can know something about what went wrong.
					var jtoken = JToken.Parse(json);
					var replyToId = jtoken["id"].ToString();
					Send(new OperationResult()
					{
						Message = message,
						ResultCode = OperationResultCode.Error
					}, replyToId);
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
				try
				{
					Conn.Send(message);
				}
				catch (Exception e)
				{
					// Don't log to App.Logger here. The WebSocket might be disconnected.
					Debug.LogError($"Error sending message {message.Payload.GetType()}\nException: {e.Message}\nStackTrace: {e.StackTrace}");
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
