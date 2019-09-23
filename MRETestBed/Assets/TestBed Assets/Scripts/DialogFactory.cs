using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MixedRealityExtension.PluginInterfaces;
using System;

public class DialogFactory : MonoBehaviour, IDialogFactory
{
	[SerializeField] private Canvas canvas;

	public void ShowDialog(string text, bool allowInput, Action<bool, string> callback)
	{
		throw new NotImplementedException();
	}

	public void OnOk()
	{
		Debug.Log("Ok");
	}

	public void OnCancel()
	{
		Debug.Log("Cancel");
	}
}
