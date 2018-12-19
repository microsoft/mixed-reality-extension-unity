// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Core.Types
{
    /// <summary>
    /// Describes the general shape of a primitive. Specifics are described in a [[PrimitiveDefinition]] object.
    /// </summary>
    public enum PrimitiveShape
    {
        /// <summary>
        /// The primitive is a sphere with a radius of [[PrimitiveDefinition.radius]], horizontal segment count
        /// [[PrimitiveDefinition.uSegments]], and vertical segment count [[PrimitiveDefinition.vSegments]], centered
        /// at the origin.
        /// </summary>
        Sphere,

        /// <summary>
        /// The primitive is a box with dimensions defined by [[PrimitiveDefinition.dimensions]], centered at the origin.
        /// </summary>
        Box,

        /// <summary>
        /// The primitive is a capsule whose central axis extends from the origin to [[PrimitiveDefinition.dimensions]], with
        /// radius [[PrimitiveDefinition.radius]], radial segment count [[PrimitiveDefinition.uSegments]], and axial
        /// segment count [[PrimitiveDefinition.vSegments]], recentered at the origin after generation.
        /// </summary>
        Capsule,

        /// <summary>
        /// The primitive is a cylinder whose central axis extends from the origin to [[PrimitiveDefinition.dimensions]], with
        /// radius [[PrimitiveDefinition.radius]], and radial segment count [[PrimitiveDefinition.uSegments]], recentered
        /// at the origin after generation.
        /// </summary>
        Cylinder,

        /// <summary>
        /// The primitive is a plane with dimensions from the x and z coordinates of [[PrimitiveDefinition.dimensions]] (y
        /// coordinate is ignored), horizontal segment count [[PrimitiveDefinition.uSegments]], and vertical segment count
        /// [[PrimitiveDefinition.vSegments]], centered at the origin.
        /// </summary>
        Plane,

        /// <summary>
        /// The primitive is a sphere with a radius of [[PrimitiveDefinition.radius]], horizontal segment count
        /// [[PrimitiveDefinition.uSegments]], and vertical segment count [[PrimitiveDefinition.vSegments]], with normals
        /// pointed inward, centered at the origin.
        /// </summary>
        InnerSphere
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
        /// The radius of spheres, cylinders, and capsules.
        /// </summary>
        public float? Radius;

        /// <summary>
        /// The size of boxes, cylinders, capsules, and planes.
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
