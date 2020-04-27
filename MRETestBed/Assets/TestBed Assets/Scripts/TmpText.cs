// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TmpText : IText
{
	private readonly TmpTextFactory _factory;
	private readonly TextMeshPro _tmp;
	private readonly Renderer _renderer;

	/// <inheritdoc />
	public bool Enabled
	{
		get { return _renderer.enabled; }
		private set { _renderer.enabled = value; }
	}

	/// <inheritdoc />
	public string Contents
	{
		get { return _tmp.text; }
		private set
		{
			_tmp.text = value;
			resizeContainer();
		}
	}

	/// <inheritdoc />
	public float Height
	{
		get { return _tmp.fontSize / 10; }
		private set
		{
			_tmp.fontSize = value * 10;
			resizeContainer();
		}
	}

	// this field is unused with TextMesh Pro
	/// <inheritdoc />
	public float PixelsPerLine { get; private set; }

	private static readonly Dictionary<TextAnchorLocation, Vector2> Pivot = new Dictionary<TextAnchorLocation, Vector2>
	{
		{ TextAnchorLocation.TopLeft, new Vector2(0, 1)},
		{ TextAnchorLocation.TopCenter, new Vector2(0.5f, 1) },
		{ TextAnchorLocation.TopRight, new Vector2(1, 1) },
		{ TextAnchorLocation.MiddleLeft, new Vector2(0, 0.5f) },
		{ TextAnchorLocation.MiddleCenter, new Vector2(0.5f, 0.5f) },
		{ TextAnchorLocation.MiddleRight, new Vector2(1, 0.5f) },
		{ TextAnchorLocation.BottomLeft, new Vector2(0, 0) },
		{ TextAnchorLocation.BottomCenter, new Vector2(0.5f, 0) },
		{ TextAnchorLocation.BottomRight, new Vector2(1, 0) }
	};

	/// <inheritdoc />
	public TextAnchorLocation Anchor
	{
		get
		{
			foreach (var kv in Pivot)
			{
				if (Mathf.Approximately(_tmp.rectTransform.pivot.x, kv.Value.x)
					&& Mathf.Approximately(_tmp.rectTransform.pivot.y, kv.Value.y))
				{
					return kv.Key;
				}
			}

			return TextAnchorLocation.TopLeft;
		}
		private set
		{
			_tmp.rectTransform.pivot = Pivot[value];
		}
	}

	/// <inheritdoc />
	public TextJustify Justify
	{
		get
		{
			switch (_tmp.alignment)
			{
				case TextAlignmentOptions.Left:
					return TextJustify.Left;
				case TextAlignmentOptions.Center:
					return TextJustify.Center;
				case TextAlignmentOptions.Right:
					return TextJustify.Right;
				default:
					return TextJustify.Left;
			}
		}
		private set
		{
			switch (value)
			{
				case TextJustify.Left:
					_tmp.alignment = TextAlignmentOptions.Left;
					break;
				case TextJustify.Center:
					_tmp.alignment = TextAlignmentOptions.Center;
					break;
				case TextJustify.Right:
					_tmp.alignment = TextAlignmentOptions.Right;
					break;
			}
		}
	}

	/// <inheritdoc />
	public FontFamily Font
	{
		get
		{
			if (_tmp.font == _factory.SerifFont)
			{
				return FontFamily.Serif;
			}
			else if (_tmp.font == _factory.SansSerifFont)
			{
				return FontFamily.SansSerif;
			}
			else if (_tmp.font == _factory.MonospaceFont)
			{
				return FontFamily.Monospace;
			}
			else if (_tmp.font == _factory.CursiveFont)
			{
				return FontFamily.Cursive;
			}
			else
			{
				return FontFamily.Default;
			}
		}
		private set
		{
			switch (value)
			{
				case FontFamily.Serif:
					_tmp.font = _factory.SerifFont != null ? _factory.SerifFont : _factory.DefaultFont;
					break;
				case FontFamily.SansSerif:
					_tmp.font = _factory.SansSerifFont != null ? _factory.SansSerifFont : _factory.DefaultFont;
					break;
				case FontFamily.Monospace:
					_tmp.font = _factory.MonospaceFont != null ? _factory.MonospaceFont : _factory.DefaultFont;
					break;
				case FontFamily.Cursive:
					_tmp.font = _factory.CursiveFont != null ? _factory.CursiveFont : _factory.DefaultFont;
					break;
				case FontFamily.Default:
				default:
					_tmp.font = _factory.DefaultFont;
					break;
			}
			_renderer.material = _tmp.font.material;
		}
	}

	/// <inheritdoc />
	public MWColor Color
	{
		get {
			return new MWColor(
				_tmp.color.r,
				_tmp.color.g,
				_tmp.color.b,
				_tmp.color.a);
		}
		private set {
			_tmp.color = new Color(
				value.R,
				value.G,
				value.B,
				value.A
			);
		}
	}

	public TmpText(TmpTextFactory factory, TextMeshPro tmp)
	{
		_factory = factory;
		_tmp = tmp;
		_renderer = tmp.gameObject.GetComponent<Renderer>();

		// set extra settings
		_tmp.richText = true;
		_tmp.enableWordWrapping = false;

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

	public void SynchronizeEngine(TextPatch patch)
	{
		ApplyPatch(patch);
	}

	private void resizeContainer()
	{
		_tmp.rectTransform.sizeDelta = new Vector2(_tmp.preferredWidth, _tmp.preferredHeight);
	}
}
