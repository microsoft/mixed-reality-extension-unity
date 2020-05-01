// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching.Types;

namespace MixedRealityExtension.Core.Interfaces
{
	/// <summary>
	/// Describes the anchor position relative to the text
	/// </summary>
	public enum TextAnchorLocation
	{
		/// <summary>
		/// Anchor text by the top left corner of the bounding box
		/// </summary>
		TopLeft,

		/// <summary>
		/// Anchor text by the top edge of the bounding box
		/// </summary>
		TopCenter,

		/// <summary>
		/// Anchor text by the top right corner of the bounding box
		/// </summary>
		TopRight,

		/// <summary>
		/// Anchor text by the left edge of the bounding box
		/// </summary>
		MiddleLeft,

		/// <summary>
		/// Anchor text by the center of the bounding box
		/// </summary>
		MiddleCenter,

		/// <summary>
		/// Anchor text by the right edge of the bounding box
		/// </summary>
		MiddleRight,

		/// <summary>
		/// Anchor text by the bottom left corner of the bounding box
		/// </summary>
		BottomLeft,

		/// <summary>
		/// Anchor text by the bottom edge of the bounding box
		/// </summary>
		BottomCenter,

		/// <summary>
		/// Anchor text by the bottom right corner of the bounding box
		/// </summary>
		BottomRight
	}

	/// <summary>
	/// Describes a line of text's horizontal position relative to the other lines
	/// </summary>
	public enum TextJustify
	{
		/// <summary>
		/// Align text lines' left edges
		/// </summary>
		Left,

		/// <summary>
		/// Align text lines' center points
		/// </summary>
		Center,

		/// <summary>
		/// Align text lines' right edges
		/// </summary>
		Right
	}

	/// <summary>
	/// A text's font
	/// </summary>
	public enum FontFamily
	{
		/// <summary>
		/// No preference on font, use engine's preferred font
		/// </summary>
		Default = 0,

		/// <summary>
		/// Use the engine's preferred serif-style font
		/// </summary>
		Serif,

		/// <summary>
		/// Use the engine's preferred sans serif-style font
		/// </summary>
		SansSerif,

		/// <summary>
		/// Use the engine's preferred monospace font
		/// </summary>
		Monospace,

		/// <summary>
		/// Use the engine's preferred handwriting font
		/// </summary>
		Cursive
	}

	/// <summary>
	/// Interface that is to be implemented to represent an engine's 3d text.
	/// </summary>
	public interface IText
	{
		/// <summary>
		/// Whether or not to draw the text
		/// </summary>
		bool Enabled { get; }

		/// <summary>
		/// The text string to be drawn
		/// </summary>
		string Contents { get; }

		/// <summary>
		/// The height in meters of a line of text
		/// </summary>
		float Height { get; }

		/// <summary>
		/// The vertical resolution of a single line of text
		/// </summary>
		float PixelsPerLine { get; }

		/// <summary>
		/// The position of the text anchor relative to the block of text
		/// </summary>
		TextAnchorLocation Anchor { get; }

		/// <summary>
		/// The alignment of each text line relative to the others
		/// </summary>
		TextJustify Justify { get; }

		/// <summary>
		/// The font family to use to draw the text
		/// </summary>
		FontFamily Font { get; }

		/// <summary>
		/// The text's color
		/// </summary>
		MWColor Color { get; }

		/// <summary>
		/// Called to apply the text patch to the object immediately.
		/// </summary>
		/// <param name="patch">The text patch to apply.</param>
		void ApplyPatch(TextPatch patch);

		/// <summary>
		/// Called to synchronize the text to the app during an app update.
		/// </summary>
		/// <param name="patch">The text patch to apply.</param>
		void SynchronizeEngine(TextPatch patch);
	}
}
