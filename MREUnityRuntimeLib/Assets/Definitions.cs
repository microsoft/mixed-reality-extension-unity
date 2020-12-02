// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching.Types;
using Newtonsoft.Json;

namespace MixedRealityExtension.Assets
{
	/// <summary>
	/// An asset definition for Node use
	/// </summary>
	public struct Asset
	{
		/// <summary>
		/// The unique ID of this asset.
		/// </summary>
		public Guid Id;

		/// <summary>
		/// A human-friendly identifier for this asset. Not guaranteed to be unique.
		/// </summary>
		public string Name;

		/// <summary>
		/// Documents the origin of this asset
		/// </summary>
		[JsonIgnore]
		public AssetSource Source;

		/// <summary>
		/// If this asset is a prefab, contains those properties
		/// </summary>
		public Prefab? Prefab;

		/// <summary>
		/// If this asset is a material, contains those properties
		/// </summary>
		public Material? Material;

		/// <summary>
		/// If this asset is a texture, contains those properties
		/// </summary>
		public Texture? Texture;

		/// <summary>
		/// If this asset is a mesh, contains those properties
		/// </summary>
		public Mesh? Mesh;

		/// <summary>
		/// If this asset is a sound, contains those properties
		/// </summary>
		public Sound? Sound;

		/// <summary>
		/// If this asset is a video, contains those properties
		/// </summary>
		public VideoStream? VideoStream;

		/// <summary>
		/// Only populated when this asset is animation data. An asset will only have one of these types specified.
		/// </summary>
		public AnimationData? AnimationData;
	}

	/// <summary>
	/// Types of asset containers
	/// </summary>
	public enum AssetContainerType
	{
		/// <summary>
		/// This asset was loaded on its own, and not from a container.
		/// </summary>
		None,

		/// <summary>
		/// Loaded from a glTF file.
		/// </summary>
		GLTF,

		/// <summary>
		/// Loaded from a host library.
		/// </summary>
		Library
	}

	/// <summary>
	/// Contains prefab asset information.
	/// </summary>
	public struct Prefab
	{
		/// <summary>
		/// The number of actors described in this prefab.
		/// </summary>
		public int ActorCount;
	}

	/// <summary>
	/// How a material's alpha channel should be used
	/// </summary>
	public enum AlphaMode
	{
		/// <summary>
		/// Draw opaque regardless of alpha
		/// </summary>
		Opaque = 0,

		/// <summary>
		/// Draw opaque, unless alpha drops below the specified cutoff
		/// </summary>
		Mask,

		/// <summary>
		/// Blend with the background by the factor of alpha
		/// </summary>
		Blend
	}
	/// <summary>
		 /// Contains material asset info
		 /// </summary>
	public struct Material
	{
		/// <summary>
		/// The main color of the material
		/// </summary>
		public ColorPatch Color;

		/// <summary>
		/// The ID of the main texture asset
		/// </summary>
		public Guid? MainTextureId;

		/// <summary>
		/// Offset the texture by this amount as a fraction of the resolution
		/// </summary>
		public Vector2Patch MainTextureOffset;

		/// <summary>
		/// Scale the texture by this amount in each axis
		/// </summary>
		public Vector2Patch MainTextureScale;

		/// <summary>
		/// The lighting-independent color
		/// </summary>
		public ColorPatch EmissiveColor;

		/// <summary>
		/// The ID of the main texture asset
		/// </summary>
		public Guid? EmissiveTextureId;

		/// <summary>
		/// Offset the texture by this amount as a fraction of the resolution
		/// </summary>
		public Vector2Patch EmissiveTextureOffset;

		/// <summary>
		/// Scale the texture by this amount in each axis
		/// </summary>
		public Vector2Patch EmissiveTextureScale;

		/// <summary>
		/// How this material should treat the color/texture alpha channel
		/// </summary>
		public AlphaMode? AlphaMode;

		/// <summary>
		/// If AlphaMode is TransparentCutout, this is the transparency threshold
		/// </summary>
		public float? AlphaCutoff;
	}

	/// <summary>
	/// Contains a basic texture description
	/// </summary>
	public struct Texture
	{
		/// <summary>
		/// The URI of the source data for this texture
		/// </summary>
		public string Uri;

		/// <summary>
		/// The resolution of the texture
		/// </summary>
		public Vector2Patch Resolution;

		/// <summary>
		/// How out-of-range U coordinates should be handled
		/// </summary>
		public UnityEngine.TextureWrapMode? WrapModeU;

		/// <summary>
		/// How out-of-range V coordinates should be handled
		/// </summary>
		public UnityEngine.TextureWrapMode? WrapModeV;
	}

	/// <summary>
	/// Contains a basic mesh description
	/// </summary>
	public struct Mesh
	{
		/// <summary>
		/// The number of vertices in this mesh
		/// </summary>
		public int VertexCount;

		/// <summary>
		/// The number of triangles in this mesh
		/// </summary>
		public int TriangleCount;

		/// <summary>
		/// The size of the mesh's axis-aligned bounding box
		/// </summary>
		public Vector3Patch BoundingBoxDimensions;

		/// <summary>
		/// The center of the mesh's axis-aligned bounding box
		/// </summary>
		public Vector3Patch BoundingBoxCenter;

		/// <summary>
		/// If this mesh is a primitive, the primitive's description
		/// </summary>
		public PrimitiveDefinition? PrimitiveDefinition;
	}

	/// <summary>
	/// Contains a basic sound description
	/// </summary>
	public struct Sound
	{
		/// <summary>
		/// The URI of the source data for this texture
		/// </summary>
		public string Uri;

		/// <summary>
		/// Duration in seconds.
		/// </summary>
		public float? Duration;
	}

	/// <summary>
	/// Contains a basic video stream description
	/// </summary>
	public struct VideoStream
	{
		/// <summary>
		/// The specific URI for the video stream
		/// This can be either Youtube://xxx, Mixer://xxx, Twitch://xxx or a regular URL
		/// </summary>
		public string Uri;

		/// <summary>
		/// Duration in seconds.
		/// </summary>
		public float? Duration;
	}
}
