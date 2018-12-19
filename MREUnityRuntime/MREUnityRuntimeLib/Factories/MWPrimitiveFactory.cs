// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using UnityEngine;
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
        private readonly Material primitiveMaterial;

        /// <summary>
        /// Create a primitive factory that uses the built-in primitive generator
        /// </summary>
        /// <param name="defaultMaterial">The material to apply to new primitives</param>
        public MWPrimitiveFactory(Material defaultMaterial)
        {
            primitiveMaterial = defaultMaterial;
        }

        /// <inheritdoc />
        public GameObject CreatePrimitive(PrimitiveDefinition definition, GameObject parent, bool addCollider)
        {
            var spawnedPrimitive = new GameObject(definition.Shape.ToString());
            spawnedPrimitive.transform.SetParent(parent.transform, false);

            MWVector3 dims = definition.Dimensions;

            MeshDraft meshDraft;
            switch (definition.Shape)
            {
                case PrimitiveShape.Sphere:
                    meshDraft = MeshDraft.Sphere(
                        definition.Radius.GetValueOrDefault(0.5f),
                        definition.USegments.GetValueOrDefault(36),
                        definition.VSegments.GetValueOrDefault(36),
                        false);
                    break;

                case PrimitiveShape.Box:
                    dims = dims ?? new MWVector3(1, 1, 1);
                    meshDraft = MeshDraft.Hexahedron(dims.X, dims.Z, dims.Y, false);
                    break;

                case PrimitiveShape.Capsule:
                    dims = dims ?? new MWVector3(0, 1, 0);
                    meshDraft = MeshDraft.Capsule(
                        dims.LargestComponentValue(),
                        definition.Radius.GetValueOrDefault(0.5f),
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
                    dims = dims ?? new MWVector3(0, 1, 0);
                    meshDraft = MeshDraft.Cylinder(
                        definition.Radius.GetValueOrDefault(0.5f),
                        definition.USegments.GetValueOrDefault(36),
                        dims.LargestComponentValue(),
                        false);

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
                    meshDraft = MeshDraft.Plane(
                        dims.X,
                        dims.Z,
                        definition.USegments.GetValueOrDefault(1),
                        definition.VSegments.GetValueOrDefault(1),
                        false);
                    meshDraft.Move(new Vector3(-dims.X / 2, 0, -dims.Z / 2));
                    break;

                case PrimitiveShape.InnerSphere:
                    meshDraft = MeshDraft.Sphere(
                        definition.Radius.GetValueOrDefault(0.5f),
                        definition.USegments.GetValueOrDefault(36),
                        definition.VSegments.GetValueOrDefault(36),
                        false);
                    meshDraft.FlipFaces();
                    break;

                default:
                    throw new Exception($"{definition.Shape.ToString()} is not a known primitive type.");
            }

            spawnedPrimitive.AddComponent<MeshFilter>().mesh = meshDraft.ToMesh();
            var renderer = spawnedPrimitive.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = primitiveMaterial;

            if (addCollider)
            {
                spawnedPrimitive.AddColliderToPrimitive(definition);
            }

            return spawnedPrimitive;
        }
    }

    static class PrimitiveHelpers
    {
        public static void AddColliderToPrimitive(this GameObject _this, PrimitiveDefinition prim)
        {
            MWVector3 dims = prim.Dimensions;
            switch (prim.Shape)
            {
                case PrimitiveShape.Sphere:
                    var sphereCollider = _this.AddComponent<SphereCollider>();
                    sphereCollider.radius = prim.Radius.GetValueOrDefault(0.5f);
                    break;

                case PrimitiveShape.Box:
                    dims = dims ?? new MWVector3(1, 1, 1);
                    var boxCollider = _this.AddComponent<BoxCollider>();
                    boxCollider.size = dims.ToVector3();
                    break;

                case PrimitiveShape.Capsule:
                    dims = dims ?? new MWVector3(0, 1, 0);
                    var capsuleCollider = _this.AddComponent<CapsuleCollider>();
                    capsuleCollider.radius = prim.Radius.GetValueOrDefault(0.5f);
                    capsuleCollider.height = dims.LargestComponentValue() + 2 * capsuleCollider.radius;
                    capsuleCollider.direction = dims.LargestComponentIndex();
                    break;

                case PrimitiveShape.Cylinder:
                    dims = dims ?? new MWVector3(0, 1, 0);
                    var cylinderCollider = _this.AddComponent<CapsuleCollider>();
                    cylinderCollider.radius = prim.Radius.GetValueOrDefault(0.5f);
                    cylinderCollider.height = dims.LargestComponentValue() + cylinderCollider.radius;
                    cylinderCollider.direction = dims.LargestComponentIndex();
                    break;

                case PrimitiveShape.Plane:
                    dims = dims ?? new MWVector3(1, 0, 1);
                    var planeCollider = _this.AddComponent<BoxCollider>();
                    planeCollider.size = new Vector3(dims.X, 0.01f, dims.Z);
                    break;
            }
        }
    }
}
