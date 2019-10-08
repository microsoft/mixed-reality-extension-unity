// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Core
{
	internal class ActorCommandQueue
	{
		#region Public Accessors

		public int Count => queue.Count;

		#endregion

		#region Constructor

		public ActorCommandQueue(Guid actorId, MixedRealityExtensionApp app)
		{
			this.actorId = actorId;
			this.app = app;
		}

		#endregion

		#region Public Methods

		public void Clear()
		{
			queue.Clear();
		}

		public void Enqueue(NetworkCommandPayload payload, Action onCompleteCallback)
		{
			queue.Enqueue(new QueuedCommand()
			{
				Payload = payload,
				OnCompleteCallback = onCompleteCallback
			});
		}

		public void Update()
		{
			if (activeCommand != null || queue.Count == 0)
			{
				return;
			}

			if (actor == null)
			{
				actor = app.FindActor(actorId) as Actor;
			}

			if (actor != null)
			{
				activeCommand = queue.Dequeue();
				try
				{
					app.ExecuteCommandPayload(actor, activeCommand.Payload, () =>
					{
						activeCommand?.OnCompleteCallback?.Invoke();
						activeCommand = null;
						Update();
					});
				}
				catch
				{
					// In case of error, clear activeCommand so that queue processing isn't stalled forever.
					activeCommand = null;
					throw;
				}
			}
		}

		#endregion

		#region Private Types

		private class QueuedCommand
		{
			public NetworkCommandPayload Payload { get; set; }
			public Action OnCompleteCallback { get; set; }
		}

		#endregion

		#region Private Fields

		private QueuedCommand activeCommand;
		private Actor actor;
		private readonly Guid actorId;
		private readonly MixedRealityExtensionApp app;
		private readonly Queue<QueuedCommand> queue = new Queue<QueuedCommand>();

		#endregion
	}
}
