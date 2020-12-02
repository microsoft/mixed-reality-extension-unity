// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces;
using UnityEngine;

namespace MixedRealityExtension.Factories
{
	/// <summary>
	/// Implements the text component as a Unity Text Mesh
	/// </summary>
	public class MWTextFactory : ITextFactory
	{
		/// <summary>
		/// The Unity Font resource used for "serif" text
		/// </summary>
		public Font SerifFont { get; private set; }

		/// <summary>
		/// The Unity Font resource used for "sans serif" text
		/// </summary>
		public Font SansSerifFont { get; private set; }

		/// <summary>
		/// Initialize a text factory
		/// </summary>
		/// <param name="serif">The Unity font resource for "serif" text</param>
		/// <param name="sansSerif">The Unity font resource for "sans serif" text</param>
		public MWTextFactory(Font serif, Font sansSerif)
		{
			SerifFont = serif;
			SansSerifFont = sansSerif;
		}

		/// <inheritdoc />
		public IText CreateText(IActor actor)
		{
			var actorGo = (Actor)actor;
			var textMesh = actorGo.gameObject.GetComponent<TextMesh>();
			if (textMesh == null)
			{
				textMesh = actorGo.gameObject.AddComponent<TextMesh>();
				textMesh.richText = false;
				textMesh.lineSpacing = 1.0f;
			}
			return actorGo.Text ?? new Text(textMesh);
		}
	}
}
