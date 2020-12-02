// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Messaging.Payloads;
using System.Collections.Generic;

namespace MixedRealityExtension.Messaging.Events
{
	internal class MWEventManager
	{
		private readonly MixedRealityExtensionApp _app;
		private readonly Queue<IMWEvent> _eventsQueue;
		private readonly Queue<IMWEvent> _lateEventsQueue;

		internal MWEventManager(MixedRealityExtensionApp app)
		{
			_app = app;
			_eventsQueue = new Queue<IMWEvent>();
			_lateEventsQueue = new Queue<IMWEvent>();
		}

		internal void ProcessEvents()
		{
			ProcessQueue(_eventsQueue, _app);
		}

		internal void ProcessLateEvents()
		{
			ProcessQueue(_lateEventsQueue, _app);
		}

		internal void QueueEvent(IMWEvent networkEvent)
		{
			_eventsQueue.Enqueue(networkEvent);
		}

		internal void QueueLateEvent(IMWEvent networkEvent)
		{
			_lateEventsQueue.Enqueue(networkEvent);
		}

		private static void ProcessQueue(Queue<IMWEvent> eventQueue, MixedRealityExtensionApp app)
		{
			if (eventQueue.Count > 0)
			{
				var payloads = new List<Payload>();
				while (eventQueue.Count != 0)
				{
					eventQueue.Dequeue().SendEvent(app);
				}
			}
		}
	}
}
