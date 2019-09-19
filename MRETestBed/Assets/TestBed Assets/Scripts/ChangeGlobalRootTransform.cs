// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeGlobalRootTransform : MonoBehaviour
{

	// Use this for initialization
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}

	int transformIndex = 0;
	float lastChangedTime;
	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag == "Player")
		{
			//if it has been more than 1/4 second since last time we entered trigger then move the root (we can't move the root every frame, because trigger entered is hit again
			if (Time.fixedTime - lastChangedTime > 0.25f)
			{
				lastChangedTime = Time.fixedTime;
				var root = gameObject.transform.parent.transform.parent;

				transformIndex++;
				switch (transformIndex)
				{
					case 1:
						root.transform.localPosition = new Vector3(1000.0f, 100.0f, 1000.0f);
						root.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
						root.transform.localScale = Vector3.one;
						break;
					case 2:
						root.transform.localPosition = Vector3.zero;
						root.transform.localRotation = Quaternion.Euler(0.0f, 30.0f, 20.0f);
						root.transform.localScale = Vector3.one;
						break;
					case 3:
						root.transform.localPosition = Vector3.zero;
						root.transform.localRotation = Quaternion.identity;
						root.transform.localScale = Vector3.one * 2.0f;
						break;
					case 4:
						root.transform.localPosition = new Vector3(-41000.0f, 9812.0f, 34817.0f);
						root.transform.localRotation = Quaternion.Euler(42.0f, 80.0f, 20.0f);
						root.transform.localScale = Vector3.one * 3.0f;
						break;
					default:
						root.transform.localPosition = Vector3.zero;
						root.transform.localRotation = Quaternion.identity;
						root.transform.localScale = Vector3.one;
						transformIndex = 0;
						break;
				}
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
