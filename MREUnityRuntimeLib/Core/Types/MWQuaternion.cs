// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Core.Types
{
	/// <summary>
	/// Class that represents a quaternion in the mixed reality extension runtime.
	/// </summary>
	public class MWQuaternion : IEquatable<MWQuaternion>
	{
		/// <summary>
		/// Gets or sets the X component of the quaternion.
		/// </summary>
		public float X { get; set; }

		/// <summary>
		/// Gets or sets the Y component of the quaternion.
		/// </summary>
		public float Y { get; set; }

		/// <summary>
		/// Gets or sets the Z component of the quaternion.
		/// </summary>
		public float Z { get; set; }

		/// <summary>
		/// Gets or sets the W component of the quaternion.
		/// </summary>
		public float W { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MWQuaternion"/> class.
		/// </summary>
		public MWQuaternion()
		{
			X = 0.0f;
			Y = 0.0f;
			Z = 0.0f;
			W = 1.0f;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MWQuaternion"/> class.
		/// </summary>
		/// <param name="quaternion">The other <see cref="MWQuaternion"/> to use for the initial value of the components for the new instance.</param>
		public MWQuaternion(MWQuaternion quaternion)
		{
			X = quaternion.X;
			Y = quaternion.Y;
			Z = quaternion.Z;
			W = quaternion.W;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MWQuaternion"/> class.
		/// </summary>
		/// <param name="w">The initial value of the X component.</param>
		/// <param name="x">The initial value of the Y component.</param>
		/// <param name="y">The initial value of the Z component.</param>
		/// <param name="z">The initial value of the W component.</param>
		public MWQuaternion(float w, float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		/// <summary>
		/// Tests for equality based on value comparisons of the quaternion components.
		/// </summary>
		/// <param name="other">The other quaternion to test equality of components against.</param>
		/// <returns>Whether the two quaternion are equal by component values.</returns>
		public bool Equals(MWQuaternion other)
		{
			return
				X.Equals(other.X) &&
				Y.Equals(other.Y) &&
				Z.Equals(other.Z) &&
				W.Equals(other.W);
		}

		/// <summary>
		/// Gets the string representation of a <see cref="MWQuaternion"/> instance.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString()
		{
			return $"{{ W: {W}, X: {X}, Y: {Y}, Z: {Z} }}";
		}
	}
}
