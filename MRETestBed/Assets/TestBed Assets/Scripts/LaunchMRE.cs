// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using UnityEngine;

public enum LaunchType
{
	MouseButtonDown,
	TriggerVolume,
	OnStart
}

public class LaunchMRE : MonoBehaviour
{
	public LaunchType LaunchType;

	public MREComponent MREComponent;

	public bool StopAppOnExit = true;

	private bool _running = false;

	// Use this for initialization
	void Start ()
	{
		
	}

	// Update is called once per frame
	void Update ()
	{
		if (!_running && LaunchType == LaunchType.OnStart)
		{
			StartApp();
		}
	}

	private void OnMouseDown()
	{
		if (LaunchType == LaunchType.MouseButtonDown && MREComponent != null)
		{
			StartApp();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (LaunchType == LaunchType.TriggerVolume && other.gameObject.tag == "Player")
		{
			StartApp();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (StopAppOnExit)
		{
			if (LaunchType == LaunchType.TriggerVolume && other.gameObject.tag == "Player")
			{
				StopApp();
			}
		}
	}

	private void StartApp()
	{
		Debug.Log("Starting MRE app.");
		MREComponent?.EnableApp();
		_running = true;
	}

	private void StopApp()
	{
		MREComponent?.DisableApp();
		_running = false;
	}
}
