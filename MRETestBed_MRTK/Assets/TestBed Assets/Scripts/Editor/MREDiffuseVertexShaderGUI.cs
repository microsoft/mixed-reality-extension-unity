// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class MREDiffuseVertexShaderGUI : ShaderGUI
{
	public enum RenderMode
	{
		Opaque = 0,
		TransparentCutout,
		Transparent
	}

	public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		//base.OnGUI(materialEditor, properties);
		foreach(var prop in properties)
		{
			if(!prop.flags.HasFlag(MaterialProperty.PropFlags.HideInInspector))
				materialEditor.ShaderProperty(prop, prop.displayName);
		}

		var mat = (Material)materialEditor.target;
		SetRenderMode(mat, (RenderMode)EditorGUILayout.EnumPopup("Render Mode", (Enum)GetRenderMode(mat)));
	}

	private RenderMode GetRenderMode(Material mat)
	{
		return mat.renderQueue == (int)RenderQueue.Geometry ? RenderMode.Opaque :
			mat.renderQueue == (int)RenderQueue.AlphaTest ? RenderMode.TransparentCutout :
			RenderMode.Transparent;
	}

	private void SetRenderMode(Material mat, RenderMode mode)
	{
		switch (mode)
		{
			case RenderMode.Opaque:
				mat.renderQueue = (int)RenderQueue.Geometry;
				mat.SetOverrideTag("RenderMode", "Opaque");
				mat.SetInt("_ZWrite", 1);
				mat.SetInt("_SrcBlend", (int)BlendMode.One);
				mat.SetInt("_DstBlend", (int)BlendMode.Zero);
				mat.SetInt("_ShouldCutout", 0);
				break;
			case RenderMode.TransparentCutout:
				mat.renderQueue = (int)RenderQueue.AlphaTest;
				mat.SetOverrideTag("RenderMode", "TransparentCutout");
				mat.SetInt("_ZWrite", 1);
				mat.SetInt("_SrcBlend", (int)BlendMode.One);
				mat.SetInt("_DstBlend", (int)BlendMode.Zero);
				mat.SetInt("_ShouldCutout", 1);
				break;
			case RenderMode.Transparent:
				mat.renderQueue = (int)RenderQueue.Transparent;
				mat.SetOverrideTag("RenderMode", "Transparent");
				mat.SetInt("_ZWrite", 0);
				mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
				mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
				mat.SetInt("_ShouldCutout", 0);
				break;
		}
	}
}
