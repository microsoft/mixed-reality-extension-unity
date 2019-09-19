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
			float radius, height;
			switch (definition.Shape)
			{
				case PrimitiveShape.Sphere:
					dims = dims ?? new MWVector3(1, 1, 1);
					meshDraft = MeshDraft.Sphere(
						definition.Dimensions.SmallestComponentValue() / 2,
						definition.USegments.GetValueOrDefault(36),
						definition.VSegments.GetValueOrDefault(18),
						true);
					break;

				case PrimitiveShape.Box:
					dims = dims ?? new MWVector3(1, 1, 1);
					meshDraft = MeshDraft.Hexahedron(dims.X, dims.Z, dims.Y, true);
					break;

				case PrimitiveShape.Capsule:
					dims = dims ?? new MWVector3(0.2f, 1, 0.2f);
					radius = definition.Dimensions.SmallestComponentValue() / 2;
					height = definition.Dimensions.LargestComponentValue() - 2 * radius;
					meshDraft = MeshDraft.Capsule(
						height,
						radius,
						definition.USegments.GetValueOrDefault(36),
						definition.VSegments.GetValueOrDefault(18));

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
					radius = 0.2f;
					height = 1;
					if (Mathf.Approximately(dims.X, dims.Y))
					{
						height = dims.Z;
						radius = dims.X / 2;
					}
					else if (Mathf.Approximately(dims.X, dims.Z))
					{
						height = dims.Y;
						radius = dims.X / 2;
					}
					else
					{
						height = dims.X;
						radius = dims.Y / 2;
					}

					meshDraft = MeshDraft.Cylinder(
						radius,
						definition.USegments.GetValueOrDefault(36),
						height,
						true);

					// default cylinder is Y-aligned; rotate if necessary
					if (dims.X == height)
					{
						meshDraft.Rotate(Quaternion.Euler(0, 0, 90));
					}
					else if (dims.Z == height)
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
						true);
					meshDraft.Move(new Vector3(-dims.X / 2, 0, -dims.Z / 2));
					break;

				default:
					throw new Exception($"{definition.Shape.ToString()} is not a known primitive type.");
			}

			return meshDraft.ToMesh();
		}
	}
}
