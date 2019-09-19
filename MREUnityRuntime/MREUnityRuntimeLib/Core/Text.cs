// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Factories;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.Unity;
using UnityEngine;

namespace MixedRealityExtension.Core
{
	internal class Text : IText
	{
		private TextMesh _tm;
		private Renderer _renderer;
		private MWColor _color = new MWColor();

		/// <inheritdoc />
		public bool Enabled
		{
			get => _renderer.enabled;
			private set => _renderer.enabled = value;
		}

		/// <inheritdoc />
		public string Contents
		{
			get => _tm.text;
			private set => _tm.text = value;
		}

		/// <inheritdoc />
		public float Height
		{
			get => _tm.characterSize * PixelsPerLine / 10;
			private set => _tm.characterSize = 10 * value / PixelsPerLine;
		}

		/// <inheritdoc />
		public float PixelsPerLine
		{
			get => _tm.fontSize;
			private set
			{
				var height = Height;
				_tm.fontSize = (int)value;
				Height = height;
			}
		}

		/// <inheritdoc />
		public TextAnchorLocation Anchor
		{
			get
			{
				return
					_tm.anchor == TextAnchor.UpperLeft ? TextAnchorLocation.TopLeft :
					_tm.anchor == TextAnchor.UpperCenter ? TextAnchorLocation.TopCenter :
					_tm.anchor == TextAnchor.UpperRight ? TextAnchorLocation.TopRight :
					_tm.anchor == TextAnchor.MiddleLeft ? TextAnchorLocation.MiddleLeft :
					_tm.anchor == TextAnchor.MiddleCenter ? TextAnchorLocation.MiddleCenter :
					_tm.anchor == TextAnchor.MiddleRight ? TextAnchorLocation.MiddleRight :
					_tm.anchor == TextAnchor.LowerLeft ? TextAnchorLocation.BottomLeft :
					_tm.anchor == TextAnchor.LowerCenter ? TextAnchorLocation.BottomCenter :
					_tm.anchor == TextAnchor.LowerRight ? TextAnchorLocation.BottomRight :
					TextAnchorLocation.TopLeft;
			}
			private set
			{
				switch (value)
				{
					case TextAnchorLocation.TopLeft: _tm.anchor = TextAnchor.UpperLeft; break;
					case TextAnchorLocation.TopCenter: _tm.anchor = TextAnchor.UpperCenter; break;
					case TextAnchorLocation.TopRight: _tm.anchor = TextAnchor.UpperRight; break;
					case TextAnchorLocation.MiddleLeft: _tm.anchor = TextAnchor.MiddleLeft; break;
					case TextAnchorLocation.MiddleCenter: _tm.anchor = TextAnchor.MiddleCenter; break;
					case TextAnchorLocation.MiddleRight: _tm.anchor = TextAnchor.MiddleRight; break;
					case TextAnchorLocation.BottomLeft: _tm.anchor = TextAnchor.LowerLeft; break;
					case TextAnchorLocation.BottomCenter: _tm.anchor = TextAnchor.LowerCenter; break;
					case TextAnchorLocation.BottomRight: _tm.anchor = TextAnchor.LowerRight; break;
				}
			}
		}

		/// <inheritdoc />
		public TextJustify Justify
		{
			get
			{
				return
					_tm.alignment == TextAlignment.Left ? TextJustify.Left :
					_tm.alignment == TextAlignment.Center ? TextJustify.Center :
					_tm.alignment == TextAlignment.Right ? TextJustify.Right :
					TextJustify.Left;
			}
			private set
			{
				switch (value)
				{
					case TextJustify.Left: _tm.alignment = TextAlignment.Left; break;
					case TextJustify.Center: _tm.alignment = TextAlignment.Center; break;
					case TextJustify.Right: _tm.alignment = TextAlignment.Right; break;
				}
			}
		}

		/// <inheritdoc />
		public FontFamily Font
		{
			get
			{
				var factory = (MWTextFactory)MREAPI.AppsAPI.TextFactory;
				return _tm.font == factory.SerifFont ? FontFamily.Serif : FontFamily.SansSerif;
			}
			private set
			{
				var factory = (MWTextFactory)MREAPI.AppsAPI.TextFactory;
				_tm.font = value == FontFamily.Serif ? factory.SerifFont : factory.SansSerifFont;
				_renderer.material = _tm.font.material;
			}
		}

		/// <inheritdoc />
		public MWColor Color
		{
			get
			{
				_color.FromUnityColor(_tm.color);
				return _color;
			}

			private set => _tm.color = value.ToColor();
		}

		internal Text(TextMesh tm)
		{
			_tm = tm;
			_renderer = _tm.gameObject.GetComponent<Renderer>();

			// set defaults
			Enabled = true;
			Contents = "";
			PixelsPerLine = 50;
			Height = 1;
			Anchor = TextAnchorLocation.TopLeft;
			Justify = TextJustify.Left;
			Font = FontFamily.SansSerif;
			Color = new MWColor(1, 1, 1, 1);
		}

		/// <inheritdoc />
		public void ApplyPatch(TextPatch patch)
		{
			Enabled = Enabled.ApplyPatch(patch.Enabled);
			Contents = Contents.ApplyPatch(patch.Contents);
			PixelsPerLine = PixelsPerLine.ApplyPatch(patch.PixelsPerLine);
			Height = Height.ApplyPatch(patch.Height);
			Anchor = Anchor.ApplyPatch(patch.Anchor);
			Justify = Justify.ApplyPatch(patch.Justify);
			Font = Font.ApplyPatch(patch.Font);
			Color = Color.ApplyPatch(patch.Color);
		}

		/// <inheritdoc />
		public void SynchronizeEngine(TextPatch patch)
		{
			ApplyPatch(patch);
		}
	}
}
