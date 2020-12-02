// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Patching.Types;
using System;

namespace MixedRealityExtension.Core.Types
{
	/// <summary>
	/// Class that represents a transform in the mixed reality extension runtime.
	/// </summary>
	public class MWTransform : IEquatable<MWTransform>
	{
		/// <summary>
		/// Gets or sets the position of the transform.
		/// </summary>
		public MWVector3 Position { get; set; }

		/// <summary>
		/// Gets or sets the rotation of the transform.
		/// </summary>
		public MWQuaternion Rotation { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MWTransform"/> class.
		/// </summary>
		public MWTransform()
		{
			Position = new MWVector3();
			Rotation = new MWQuaternion();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MWTransform"/> class.
		/// </summary>
		/// <param name="transform">The other <see cref="MWTransform"/> to use for the initial value of the components for the new instance.</param>
		public MWTransform(MWTransform transform)
		{
			Position = transform.Position;
			Rotation = transform.Rotation;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MWTransform"/> class.
		/// </summary>
		/// <param name="position">The position of the new transform.</param>
		/// <param name="rotation">The rotation of the new transform.</param>
		public MWTransform(MWVector3 position, MWQuaternion rotation)
		{
			Position = position;
			Rotation = rotation;
		}

		/// <summary>
		/// Tests for equality based on value comparisons of the transform components.
		/// </summary>
		/// <param name="other">The other transform to test equality of components against.</param>
		/// <returns>Whether the two transforms are equal by component values.</returns>
		public virtual bool Equals(MWTransform other)
		{
			return
				Position.Equals(other.Position) &&
				Rotation.Equals(other.Rotation);
		}

		internal virtual TransformPatch AsPatch()
		{
			return new TransformPatch()
			{
				Position = new Vector3Patch(Position),
				Rotation = new QuaternionPatch(Rotation)
			};
		}

		/// <summary>
		/// Gets the string representation of a <see cref="MWTransform"/> instance.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString()
		{
			return $"Transform:\n\tPosition: {Position.ToString()}\n\tRotation: {Rotation.ToString()}";
		}
	}

	/// <summary>
	/// Class that represents a scaled transform in the mixed reality extension runtime.
	/// </summary>
	public class MWScaledTransform: MWTransform
	{
		/// <summary>
		/// Gets or sets the scale of the transform.
		/// </summary>
		public MWVector3 Scale { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MWScaledTransform"/> class.
		/// </summary>
		public MWScaledTransform()
			: base()
		{
			Scale = new MWVector3();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MWScaledTransform"/> class.
		/// </summary>
		/// <param name="transform">The other <see cref="MWScaledTransform"/> to use for the initial value of the components for the new instance.</param>
		public MWScaledTransform(MWScaledTransform transform): base(transform)
		{
			Scale = transform.Scale;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MWScaledTransform"/> class.
		/// </summary>
		/// <param name="position">The position of the new scaled transform.</param>
		/// <param name="rotation">The rotation of the new scaled transform.</param>
		/// <param name="scale">The scale of the new scaled transform.</param>
		public MWScaledTransform(MWVector3 position, MWQuaternion rotation, MWVector3 scale)
			: base(position, rotation)
		{
			Scale = scale;
		}

		/// <summary>
		/// Tests for equality based on value comparisons of the scaled transform components.
		/// </summary>
		/// <param name="other">The other scaled transform to test equality of components against.</param>
		/// <returns>Whether the two scaled transforms are equal by component values.</returns>
		public bool Equals(MWScaledTransform other)
		{
			return
				base.Equals(other) &&
				Scale.Equals(other.Scale);
		}

		/// <summary>
		/// Gets the string representation of a <see cref="MWScaledTransform"/> instance.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString()
		{
			return $"Transform:\n\tPosition: {Position.ToString()}\n\tRotation: {Rotation.ToString()}\n\tScale: {Scale.ToString()}";
		}

		internal new ScaledTransformPatch AsPatch()
		{
			return new ScaledTransformPatch()
			{
				Position = new Vector3Patch(Position),
				Rotation = new QuaternionPatch(Rotation),
				Scale = new Vector3Patch(Scale)
			};
		}
	}
}
