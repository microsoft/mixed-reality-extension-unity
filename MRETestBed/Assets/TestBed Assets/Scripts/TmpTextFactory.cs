// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces;
using TMPro;
using UnityEngine;

public class TmpTextFactory : ITextFactory
{
	public TMP_FontAsset DefaultFont;
	public TMP_FontAsset SerifFont;
	public TMP_FontAsset SansSerifFont;
	public TMP_FontAsset MonospaceFont;
	public TMP_FontAsset CursiveFont;

	public IText CreateText(IActor actor)
	{
		var actorGo = actor.GameObject;
		var tmp = actorGo.GetComponent<TextMeshPro>();
		if (tmp == null)
		{
			tmp = actorGo.AddComponent<TextMeshPro>();
		}

		return new TmpText(this, tmp);
	}
}
