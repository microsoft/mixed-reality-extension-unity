// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MixedRealityExtension.App;
using MixedRealityExtension.PluginInterfaces;
using System;

public class DialogFactory : MonoBehaviour, IDialogFactory
{
	[SerializeField] private Canvas canvas;
	[SerializeField] private UnityStandardAssets.Characters.FirstPerson.FirstPersonController controller;
	[SerializeField] private Assets.Scripts.User.InputSource inputSource;
	[SerializeField] private Text label;
	[SerializeField] private InputField input;

	private class DialogQueueEntry
	{
		public string text;
		public bool allowInput;
		public Action<bool, string> callback;
	}

	private Queue<DialogQueueEntry> queue = new Queue<DialogQueueEntry>(3);
	private DialogQueueEntry activeDialog;

	private void Start()
	{
		Hide();
	}

	public void ShowDialog(IMixedRealityExtensionApp app, string text, bool acceptInput, Action<bool, string> callback)
	{
		queue.Enqueue(new DialogQueueEntry() { text = text, allowInput = acceptInput, callback = callback });
		ProcessQueue();
	}

	private void ProcessQueue()
	{
		if (queue.Count == 0 || canvas.enabled) return;

		activeDialog = queue.Dequeue();
		label.text = activeDialog.text;
		input.gameObject.SetActive(activeDialog.allowInput);
		input.text = "";

		canvas.enabled = true;
		controller.enabled = false;
		inputSource.enabled = false;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	private void OnOk()
	{
		try
		{
			activeDialog.callback?.Invoke(true, activeDialog.allowInput ? input.text : null);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
		finally
		{
			activeDialog = null;
		}

		Hide();
	}

	private void OnCancel()
	{
		try
		{
			activeDialog.callback?.Invoke(false, null);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
		finally
		{
			activeDialog = null;
		}

		Hide();
	}

	private void Hide()
	{
		canvas.enabled = false;
		controller.enabled = true;
		inputSource.enabled = true;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		ProcessQueue();
	}
}
