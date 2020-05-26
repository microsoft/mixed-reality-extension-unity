// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MREComponent))]
public class MREAppEditor : Editor {

	private bool state;
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		EditorGUILayout.Space();
		if (GUILayout.Button("Connect"))
		{
			(target as MREComponent)?.EnableApp();
		}

		if (GUILayout.Button("Disconnect"))
		{
			(target as MREComponent)?.DisableApp();
		}
	}
}
