// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace MixedRealityExtension.Core.Types
{
	/// <summary>
	/// Describes the general shape of a primitive. Specifics are described in a [[PrimitiveDefinition]] object.
	/// Docs for shape-specific stuff are in the SDK: https://microsoft.github.io/mixed-reality-extension-sdk/enums/primitiveshape.html
	/// </summary>
	public enum PrimitiveShape
	{
		Sphere,
		Box,
		Capsule,
		Cylinder,
		Plane
	}

	/// <summary>
	/// The size, shape, and description of a primitive.
	/// </summary>
	public struct PrimitiveDefinition
	{
		/// <summary>
		/// The general shape of the defined primitive.
		/// </summary>
		public PrimitiveShape Shape;

		/// <summary>
		/// The bounding box of the primitive.
		/// </summary>
		public MWVector3 Dimensions;

		/// <summary>
		/// The number of horizontal or radial segments of spheres, cylinders, capsules, and planes.
		/// </summary>
		public int? USegments;

		/// <summary>
		/// The number of vertical or axial segments of spheres, capsules, and planes.
		/// </summary>
		public int? VSegments;
	}
}
