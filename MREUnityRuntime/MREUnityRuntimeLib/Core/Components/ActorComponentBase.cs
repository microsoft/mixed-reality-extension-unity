// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using UnityEngine;

namespace MixedRealityExtension.Core.Components
{
	internal class ActorComponentBase : MonoBehaviour
	{
		internal Actor AttachedActor { get; set; }

		internal virtual void CleanUp()
		{

		}

		internal virtual void SynchronizeComponent()
		{

		}

		private void Start()
		{
			if (AttachedActor == null)
			{
				AttachedActor = gameObject.GetComponent<Actor>() ??
					throw new NullReferenceException("Game object must have an actor script on it if it is going to have an actor component on it.");
			}
		}
	}
}
