// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using UnityEngine;
using MixedRealityExtension.API;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.ProceduralToolkit;
using MixedRealityExtension.Util.Unity;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core;

namespace MixedRealityExtension.Factories
{
    public class MWPrimitiveFactory : IPrimitiveFactory
    {
        /// <inheritdoc />
        public Mesh CreatePrimitive(PrimitiveDefinition definition)
        {
            MWVector3 dims = definition.Dimensions;

            MeshDraft meshDraft;
            switch (definition.Shape)
            {
                case PrimitiveShape.Sphere:
                    dims = dims ?? new MWVector3(1, 1, 1);
                    meshDraft = MeshDraft.Sphere(
                        definition.Dimensions.SmallestComponentValue() / 2,
                        definition.USegments.GetValueOrDefault(36),
                        definition.VSegments.GetValueOrDefault(36),
                        true);
                    break;

                case PrimitiveShape.Box:
                    dims = dims ?? new MWVector3(1, 1, 1);
                    meshDraft = MeshDraft.Hexahedron(dims.X, dims.Z, dims.Y, true);
                    break;

                case PrimitiveShape.Capsule:
                    dims = dims ?? new MWVector3(0.2f, 1, 0.2f);
                    meshDraft = MeshDraft.Capsule(
                        dims.LargestComponentValue(),
                        definition.Dimensions.SmallestComponentValue() / 2,
                        definition.USegments.GetValueOrDefault(36),
                        definition.VSegments.GetValueOrDefault(36));

                    // default capsule is Y-aligned; rotate if necessary
                    if (dims.LargestComponentIndex() == 0)
                    {
                        meshDraft.Rotate(Quaternion.Euler(0, 0, 90));
                    }
                    else if (dims.LargestComponentIndex() == 2)
                    {
                        meshDraft.Rotate(Quaternion.Euler(90, 0, 0));
                    }
                    break;

                case PrimitiveShape.Cylinder:
                    dims = dims ?? new MWVector3(0.2f, 1, 0.2f);
                    meshDraft = MeshDraft.Cylinder(
                        definition.Dimensions.SmallestComponentValue() / 2,
                        definition.USegments.GetValueOrDefault(36),
                        dims.LargestComponentValue(),
                        true);

                    // default cylinder is Y-aligned; rotate if necessary
                    if (dims.LargestComponentIndex() == 0)
                    {
                        meshDraft.Rotate(Quaternion.Euler(0, 0, 90));
                    }
                    else if (dims.LargestComponentIndex() == 2)
                    {
                        meshDraft.Rotate(Quaternion.Euler(90, 0, 0));
                    }
                    break;

                case PrimitiveShape.Plane:
                    dims = dims ?? new MWVector3(1, 0, 1);
                    var longEdge = dims.LargestComponentValue();
                    var shortEdge = dims.SecondLargestComponentValue();
                    meshDraft = MeshDraft.Plane(
                        longEdge,
                        shortEdge,
                        definition.USegments.GetValueOrDefault(1),
                        definition.VSegments.GetValueOrDefault(1),
                        true);
                    meshDraft.Move(new Vector3(-longEdge / 2, 0, -shortEdge / 2));

                    // rotate to orient X and Z to specified long and short axes facing positive third axis
                    Quaternion quat;
                    if (longEdge == dims.X)
                    {
                        if (shortEdge == dims.Y)
                            quat = Quaternion.Euler(90, 0, 0); // X long, Y short, facing +Z
                        else
                            quat = Quaternion.Euler(0, 0, 0); // X long, Z short, facing +Y
                    }
                    else if (longEdge == dims.Y)
                    {
                        if (shortEdge == dims.X)
                            quat = Quaternion.Euler(90, 90, 0); // Y long, X short, facing +Z
                        else
                            quat = Quaternion.Euler(0, 0, -90); // Y long, Z short, facing +X
                    }
                    else
                    {
                        if (shortEdge == dims.X)
                            quat = Quaternion.Euler(0, 90, 0); // Z long, X short, facing +Y
                        else
                            quat = Quaternion.Euler(0, 90, -90); // Z long, Y short, facing +X
                    }
                    meshDraft.Rotate(quat);
                    break;

                case PrimitiveShape.InnerSphere:
                    dims = dims ?? new MWVector3(1, 1, 1);
                    meshDraft = MeshDraft.Sphere(
                        definition.Dimensions.SmallestComponentValue() / 2,
                        definition.USegments.GetValueOrDefault(36),
                        definition.VSegments.GetValueOrDefault(36),
                        true);
                    meshDraft.FlipFaces();
                    break;

                default:
                    throw new Exception($"{definition.Shape.ToString()} is not a known primitive type.");
            }

            return meshDraft.ToMesh();
        }
    }
}
