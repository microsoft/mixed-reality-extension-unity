// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Assets.TestBed_Assets.Scripts.UserInput;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Behaviors
{
	public abstract class MRTKBehaviorBase : MonoBehaviour, IBehavior
	{
		public IActor Actor { get; set; }

		public IUser GetMWUnityUser()
		{
			var userGameObject = MREInputManager.Instance.UserGameObject;

			if (userGameObject == null)
			{
				Debug.LogError("MRE Input Manager does not have a user game object assigned to it.");
				return null;
			}

			return userGameObject.GetComponents<IUser>()
				.Where(user => user.AppInstanceId == Actor.AppInstanceId)
				.FirstOrDefault();
		}

		public void CleanUp()
		{
			DisposeActions();
			DestroyImmediate(this);
		}

		private void Start()
		{
			InitializeActions();
		}

		private void OnDestroy()
		{
			DisposeActions();
		}

		protected abstract void InitializeActions();

		protected abstract void DisposeActions();
	}
}
