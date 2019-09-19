// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserJoinApp : MonoBehaviour {

	public MREComponent MREComponent;

	private const string UserId = "ReadyPlayerOne";

	private bool appRunning;
	private bool userJoined;
	private bool userInVolume;

	// Use this for initialization
	void Start () {
		if (MREComponent != null)
		{
			MREComponent.OnAppStarted += MREComponent_OnAppStarted;
			MREComponent.OnAppShutdown += MREComponent_OnAppShutdown;
		}
	}

	private void MREComponent_OnAppStarted(MREComponent app)
	{
		appRunning = true;
		ProcessJoin();
	}

	private void MREComponent_OnAppShutdown(MREComponent app)
	{
		appRunning = false;
		userJoined = false;
	}

	// Update is called once per frame
	void Update () {
		
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag == "Player")
		{
			userInVolume = true;
			ProcessJoin();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.tag == "Player")
		{
			userInVolume = false;
			ProcessLeave();
		}
	}

	private void ProcessJoin()
	{
		if (!userJoined && appRunning && userInVolume)
		{
			Join();
		}
	}

	private void ProcessLeave()
	{
		if (userJoined && appRunning && !userInVolume)
		{
			Leave();
		}
	}

	private void Join()
	{
		if (MREComponent != null)
		{
			MREComponent.UserJoin();
			userJoined = true;
		}
	}

	private void Leave()
	{
		if (MREComponent != null)
		{
			MREComponent.UserLeave();
			userJoined = false;
		}
	}
}
