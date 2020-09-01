// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MixedRealityExtension.App;
using MixedRealityExtension.PluginInterfaces;
using System;
using UnityEditor;
using MixedRealityExtension.Assets;

public class DialogFactory : MonoBehaviour, IDialogFactory
{

	private GameObject baseCanvasGameObject;
	private GameObject activeCanvasGameObject;
	[SerializeField] public UnityStandardAssets.Characters.FirstPerson.FirstPersonController controller;
	[SerializeField] private Assets.Scripts.User.InputSource inputSource;
	private Canvas activeCanvas;
	private Text activePromptText;
	private InputField activeInputField;
	private Text activeInputText;
	private GameObject cancelButton;
	private GameObject submitButton;
	private GameObject okButton;
	private Button cancelButtonScript;
	private Button submitButtonScript;
	private Button okButtonScript;

	private class DialogQueueEntry
	{
		public string text;
		public bool allowInput;
		public Action<bool, string> callback;
	}

	private Queue<DialogQueueEntry> queue = new Queue<DialogQueueEntry>(3);
	private DialogQueueEntry activeDialog;

	private void Update()
	{
		if (activeCanvas != null && activeCanvas.enabled && Input.GetKeyDown(KeyCode.Return))
		{
			OnSubmit();
		}

	}
	private void Start()
	{
		if (activeCanvasGameObject == null)
		{
			//baseCanvasGameObject = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Testbed Assets/Prefabs/PromptTemplate.prefab", typeof(GameObject));
			activeCanvasGameObject = this.transform.gameObject;
			activeCanvas = (Canvas)activeCanvasGameObject.GetComponent("Canvas");
			activeCanvas.enabled = false;
			activePromptText = (Text)activeCanvasGameObject.transform.Find("PromptText").GetComponent("Text");
			activeInputField = (InputField)activeCanvasGameObject.transform.Find("PromptInput").GetComponent<InputField>();
			activeInputField.enabled = true;
			activeInputText = (Text)activeCanvasGameObject.transform.Find("PromptInput").transform.Find("Text").GetComponent("Text");

			cancelButton = activeCanvasGameObject.transform.Find("CancelButton").gameObject;
			cancelButtonScript = (Button)cancelButton.GetComponent("Button");
			cancelButtonScript.onClick.AddListener(OnCancel);
			submitButton = activeCanvasGameObject.transform.Find("SubmitButton").gameObject;
			submitButtonScript = (Button)submitButton.GetComponent("Button");
			submitButtonScript.onClick.AddListener(OnSubmit);
			okButton = activeCanvasGameObject.transform.Find("OKButton").gameObject;
			okButtonScript = (Button)okButton.GetComponent("Button");
			okButtonScript.onClick.AddListener(OnOk);
		}
		Hide();
	}

	public void ShowDialog(IMixedRealityExtensionApp app, string text, bool acceptInput, Action<bool, string> callback)
	{ 
		queue.Enqueue(new DialogQueueEntry() { text = text, allowInput = acceptInput, callback = callback });

		if(acceptInput)
		{
			okButton.SetActive(false);
			submitButton.SetActive(true);
			cancelButton.SetActive(true);
		}
		else
		{
			okButton.SetActive(true);
			submitButton.SetActive(false);
			cancelButton.SetActive(false);
		}

		ProcessQueue();
	}

	private void ProcessQueue()
	{
		if (queue.Count == 0 || activeCanvas.enabled) return;

		activeDialog = queue.Dequeue();
		activePromptText.text = activeDialog.text;
		activeInputField.ActivateInputField();
		activeInputField.text = "";

		if(activeCanvas)
			activeCanvas.enabled = true;
		if(controller)
			controller.enabled = false;

		//inputSource.enabled = false;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	private void OnOk()
	{
		activeDialog = null;
		Hide();
	}

	private void OnSubmit()
	{
		try
		{
			string text = activeInputField.text;
			activeDialog.callback?.Invoke(true, text);
			activeInputField.text = "";
			activeInputField.enabled = false;
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
			activeInputField.text = "";
			activeInputField.enabled = false;
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
		if(activeCanvas)
			activeCanvas.enabled = false;
		if(controller)
			controller.enabled = true;

		

		//inputSource.enabled = true;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		ProcessQueue();
	}
}
