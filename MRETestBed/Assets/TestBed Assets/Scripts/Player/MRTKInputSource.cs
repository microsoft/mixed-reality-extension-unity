// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.TestBed_Assets.Scripts.UserInput;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.TestBed_Assets.Scripts.Player
{
	public class MRTKInputSource : MonoBehaviour
	{
		public GameObject UserGameObject;

		private void Start()
		{
			Initialize();
		}

		private async void Initialize()
		{
			await MREInputManager.CreateManager();
			MREInputManager.Instance.UserGameObject = UserGameObject;
		}
	}
}
