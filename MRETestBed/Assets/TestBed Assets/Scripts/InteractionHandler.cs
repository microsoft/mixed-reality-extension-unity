// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#if UNITY_WSA
using UnityEngine.XR.WSA.Input;
#endif

public class InteractionHandler : MonoBehaviour
{


#if UNITY_WSA
	private GestureRecognizer gr;
#endif
	public MREComponent theComponent;
	private bool isCurrentlyInFocus;
	private bool isCurrentlySelected;
	private Material material;
	private Color initColor;
	// Use this for initialization
	void Start()
	{
		material = GetComponent<Renderer>().material;
		initColor = material.color;
		if (theComponent == null)
		{
			theComponent = GetComponent<MREComponent>();
		}

		if (theComponent == null)
		{
			Debug.LogError("unable to find a Mixed Reality Extension component");
			return;
		}

		isCurrentlyInFocus = false;
#if UNITY_WSA

		gr = new GestureRecognizer();
		gr.SetRecognizableGestures(GestureSettings.Tap);
		gr.Tapped += Gr_Tapped;
		gr.HoldStarted += Gr_HoldStarted;

		gr.StartCapturingGestures();
#endif
	}

#if UNITY_WSA
	private void App_OnAppStarted(MREComponent app)
	{
		app.OnAppStarted -= App_OnAppStarted;
		if (!app.AutoJoin)
		{
			app.UserJoin();
		}

		StartCoroutine(SetCurrentAsActive());
	}

	IEnumerator SetCurrentAsActive()
	{
		// since we dont have a mechanism to know if an app has finished playing. just keep the selected state for a fixed time
		yield return new WaitForSeconds(15);
		isCurrentlySelected = false;

	}

	private void StartApp()
	{
		if (isCurrentlyInFocus && theComponent != null && !isCurrentlySelected)
		{
			isCurrentlySelected = true;
			material.color = Color.green;

			theComponent.DisableApp();
			theComponent.EnableApp();
			theComponent.OnAppStarted += App_OnAppStarted;
		}
	}

	private void Gr_HoldStarted(HoldStartedEventArgs obj)
	{
		StartApp();
	}

	private void Gr_Tapped(TappedEventArgs obj)
	{
		StartApp();
	}
#endif

	public void SetFocus(bool inFocus)
	{
		if (!isCurrentlySelected)
		{
			if (inFocus)
			{
				material.color = Color.yellow;
			}
			else
			{
				material.color = initColor;
			}
		}
		isCurrentlyInFocus = inFocus;
	}
}
