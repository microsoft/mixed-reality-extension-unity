// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Core.Types
{
	/// <summary>
	/// Class that represents the color type in the Mixed Reality Extension runtime.
	/// </summary>
	public class MWColor : IEquatable<MWColor>
	{
		/// <summary>
		/// Gets or sets the red value of the color.
		/// </summary>
		public float R { get; set; }

		/// <summary>
		/// Gets or sets the green value of the color.
		/// </summary>
		public float G { get; set; }

		/// <summary>
		/// Gets or sets the blue value of the color.
		/// </summary>
		public float B { get; set; }

		/// <summary>
		/// Gets or sets the alpha value of the color.
		/// </summary>
		public float A { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MWColor"/> class.
		/// </summary>
		public MWColor()
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="MWColor"/> class.
		/// </summary>
		/// <param name="color">The other <see cref="MWColor"/> to use for the initial value of the components for the new instance.</param>
		public MWColor(MWColor color)
		{
			R = color.R;
			G = color.G;
			B = color.B;
			A = color.A;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MWColor"/> class.
		/// </summary>
		/// <param name="r">The initial red value.</param>
		/// <param name="g">The initial green value.</param>
		/// <param name="b">The initial blue value.</param>
		/// <param name="a">The initial alpha value.</param>
		public MWColor(float r, float g, float b, float a)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}

		/// <summary>
		/// Tests for equality based on value comparisons of the color components.
		/// </summary>
		/// <param name="other">The other color to test equality of components against.</param>
		/// <returns>Whether the two color are equal by component values.</returns>
		public bool Equals(MWColor other)
		{
			return
				R.Equals(other.R) &&
				G.Equals(other.G) &&
				B.Equals(other.B) &&
				A.Equals(other.A);
		}
	}
}
