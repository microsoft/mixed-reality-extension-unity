// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Core.Types
{
	/// <summary>
	/// Class that represents a 2D vector within the Mixed Reality Extension runtime.
	/// </summary>
	public class MWVector2 : IEquatable<MWVector2>
	{
		/// <summary>
		/// Get or sets the X component of the vector.
		/// </summary>
		public float X { get; set; }

		/// <summary>
		/// Get or sets the Y component of the vector.
		/// </summary>
		public float Y { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MWVector2"/> class.
		/// </summary>
		public MWVector2()
		{
			X = 0.0f;
			Y = 0.0f;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MWVector2"/> class.
		/// </summary>
		/// <param name="vector">The other <see cref="MWVector2"/> to use for the initial value of the components for the new instance.</param>
		public MWVector2(MWVector2 vector)
		{
			X = vector.X;
			Y = vector.Y;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MWVector2"/> class.
		/// </summary>
		/// <param name="x">The initial value of the X component.</param>
		/// <param name="y">The initial value of the Y component.</param>
		public MWVector2(float x, float y)
		{
			X = x;
			Y = y;
		}

		/// <summary>
		/// Tests for equality based on value comparisons of the vector components.
		/// </summary>
		/// <param name="other">The other vector to test equality of components against.</param>
		/// <returns>Whether the two vector are equal by component values.</returns>
		public bool Equals(MWVector2 other)
		{
			return
				X.Equals(other.X) &&
				Y.Equals(other.Y);
		}

		/// <summary>
		/// Gets the string representation of a <see cref="MWVector2"/> instance.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString()
		{
			return $"{{ X: {X}, Y: {Y} }}";
		}
	}
}
