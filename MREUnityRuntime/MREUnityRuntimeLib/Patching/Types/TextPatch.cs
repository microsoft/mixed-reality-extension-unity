// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace MixedRealityExtension.Patching.Types
{
	public class TextPatch : Patchable<TextPatch>
	{
		[PatchProperty]
		public bool? Enabled { get; set; }

		[PatchProperty]
		public string Contents { get; set; }

		[PatchProperty]
		public float? Height { get; set; }

		[PatchProperty]
		public float? PixelsPerLine { get; set; }

		[PatchProperty]
		public TextAnchorLocation? Anchor { get; set; }

		[PatchProperty]
		public TextJustify? Justify { get; set; }

		[PatchProperty]
		public FontFamily? Font { get; set; }

		[PatchProperty]
		public ColorPatch Color { get; set; }

		public TextPatch() { }

		internal TextPatch(IText text)
		{
			Enabled = text.Enabled;
			Contents = text.Contents;
			Height = text.Height;
			PixelsPerLine = text.PixelsPerLine;
			Anchor = text.Anchor;
			Justify = text.Justify;
			Font = text.Font;
			Color = new ColorPatch(text.Color);
		}
	}
}
