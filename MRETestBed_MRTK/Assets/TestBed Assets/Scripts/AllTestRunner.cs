// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllTestRunner : MonoBehaviour
{
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{

	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag == "Player")
		{
			var apps = FindObjectsOfType<MREComponent>();
			foreach (var app in apps)
			{
				app.EnableApp();
				app.OnAppStarted += App_OnAppStarted;
			}
		}
	}

	private void App_OnAppStarted(MREComponent app)
	{
		app.OnAppStarted -= App_OnAppStarted;
		if (!app.AutoJoin)
		{
			app.UserJoin();
		}
	}
}
