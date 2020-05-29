// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Behaviors
{
	public abstract class BehaviorBase : MonoBehaviour, IBehavior
	{
		public IActor Actor { get; set; }

		public abstract Type GetDesiredToolType();

		public IUser GetMWUnityUser(GameObject userGameObject)
		{
			return userGameObject.GetComponents<IUser>()
				.Where(user => user.AppInstanceId == Actor.AppInstanceId)
				.FirstOrDefault();
		}

		public void CleanUp()
		{
			DestroyImmediate(this);
		}
	}
}
