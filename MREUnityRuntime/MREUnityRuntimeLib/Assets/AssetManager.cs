// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using MixedRealityExtension.API;
using MixedRealityExtension.Util;
using UnityEngine;
using Object = UnityEngine.Object;

using ColliderGeometry = MixedRealityExtension.Core.ColliderGeometry;
using AssetCallback = System.Action<MixedRealityExtension.Assets.AssetManager.AssetMetadata>;

namespace MixedRealityExtension.Assets
{
	/// <summary>
	/// Keep track of all ready-to-use assets in this MRE instance
	/// </summary>
	public class AssetManager
	{
		/// <summary>
		/// Stores all the necessary info about an asset, including where it came from. Source will be null if this
		/// is not a shared asset, i.e. is a one-off creation of an MRE, or is a modified copy of something from
		/// the asset cache.
		/// </summary>
		public struct AssetMetadata
		{
			public readonly Guid Id;
			public readonly Guid ContainerId;
			public readonly Object Asset;
			public readonly ColliderGeometry ColliderGeometry;
			public readonly AssetSource Source;

			public AssetMetadata(Guid id, Guid containerId, Object asset,
				ColliderGeometry collider = null, AssetSource source = null)
			{
				Id = id;
				ContainerId = containerId;
				Source = source;
				Asset = asset;
				ColliderGeometry = collider;
			}
		}

		public event Action<Guid> AssetReferenceChanged;

		private App.IMixedRealityExtensionApp App;
		private readonly Dictionary<Guid, AssetMetadata> Assets = new Dictionary<Guid, AssetMetadata>(50);
		private readonly Dictionary<Guid, List<AssetCallback>> Callbacks
			= new Dictionary<Guid, List<AssetCallback>>(50);
		private readonly GameObject cacheRoot;
		private readonly GameObject emptyTemplate;

		public AssetManager(App.IMixedRealityExtensionApp app, GameObject root = null)
		{
			App = app;
			cacheRoot = root ?? new GameObject("MRE Cache Root");
			cacheRoot.SetActive(false);

			emptyTemplate = new GameObject("Empty");
			emptyTemplate.transform.SetParent(cacheRoot.transform, false);
		}

		/// <summary>
		/// The game object in the scene hierarchy that should be used as parent for any assets that require one,
		/// i.e. Prefabs.
		/// </summary>
		/// <returns></returns>
		public GameObject CacheRootGO()
		{
			return cacheRoot;
		}

		/// <summary>
		/// The game object that should be duplicated for new actors.
		/// </summary>
		/// <returns></returns>
		public GameObject EmptyTemplate()
		{
			return emptyTemplate;
		}

		/// <summary>
		/// Retrieve an asset by ID
		/// </summary>
		/// <param name="id">The ID of the asset to look up</param>
		/// <param name="writeSafe">If true, and the stored asset with that ID is shared,
		/// a copy of the asset will be made, and stored back into the manager. Any other shared assets that reference
		/// this asset will also be recursively copied and stored back. Each copied asset will have the original
		/// returned to the cache, decrementing the original's reference count.</param>
		/// <returns></returns>
		public AssetMetadata? GetById(Guid? id, bool writeSafe = false)
		{
			if (id != null && Assets.TryGetValue(id.Value, out AssetMetadata metadata))
			{
				// copy sourced assets if requesting write-safe
				if (writeSafe)
				{
					MakeWriteSafe(metadata);
				}
				return Assets[id.Value];
			}
			else return null;
		}

		/// <summary>
		/// Retrieve an asset's metadata from the asset reference itself.
		/// </summary>
		/// <param name="asset"></param>
		/// <returns></returns>
		public AssetMetadata? GetByObject(Object asset)
		{
			foreach (var metadata in Assets.Values)
			{
				if (metadata.Asset == asset)
				{
					return metadata;
				}
			}
			return null;
		}

		/// <summary>
		/// Be notified when an asset is finished loading and available for use.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="callback"></param>
		public void OnSet(Guid id, AssetCallback callback)
		{
			var asset = GetById(id);
			if (asset != null)
			{
				try
				{
					callback?.Invoke(asset.Value);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
			else
			{
				Callbacks.GetOrCreate(id, () => new List<AssetCallback>(10)).Add(callback);
			}
		}

		/// <summary>
		/// Track a new asset reference. Will be called during asset creation, after the asset content is downloaded
		/// or retrieved from cache.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="containerId"></param>
		/// <param name="asset"></param>
		/// <param name="colliderGeo"></param>
		/// <param name="source"></param>
		public void Set(Guid id, Guid containerId, Object asset,
			ColliderGeometry colliderGeo = null, AssetSource source = null)
		{
			if (!Assets.ContainsKey(id))
			{
				Assets[id] = new AssetMetadata(id, containerId, asset, colliderGeo, source);
			}

			if (Callbacks.TryGetValue(id, out List<AssetCallback> callbacks))
			{
				Callbacks.Remove(id);
				foreach (var cb in callbacks)
				{
					try
					{
						cb?.Invoke(Assets[id]);
					}
					catch(Exception e)
					{
						Debug.LogException(e);
					}
				}
			}
		}

		/// <summary>
		/// Break references to all shared assets and destroy all unshared assets with this container ID.
		/// </summary>
		/// <param name="containerId"></param>
		public void Unload(Guid containerId)
		{
			var assets = Assets.Values.Where(c => c.ContainerId == containerId && c.Asset != null).ToArray();
			for (var i = 0; i < assets.Length; i++)
			{
				var asset = assets[i];
				Assets.Remove(asset.Id);

				// asset is a one-off, just destroy it
				if (asset.Source == null)
				{
					// workaround: unload meshes implicitly
					if (asset.Asset is GameObject prefab)
					{
						var filters = prefab.GetComponentsInChildren<MeshFilter>();
						foreach (var f in filters)
						{
							Object.Destroy(f.sharedMesh);
						}
					}
					Object.Destroy(asset.Asset);
				}
				// asset is shared with other MRE instances, just return asset to cache
				else
				{
					MREAPI.AppsAPI.AssetCache.StoreAssets(
						asset.Source.ParsedUri,
						new Object[]{ asset.Asset },
						asset.Source.Version);
				}
			}
		}

		/// <summary>
		/// Recursively copy shared assets from cache into manager so the app can modify them.
		/// </summary>
		/// <param name="metadata"></param>
		private void MakeWriteSafe(AssetMetadata metadata, AssetMetadata? dependency = null, AssetMetadata? updatedDependency = null)
		{
			if (metadata.Source == null) return;

			// copy asset
			var originalAsset = metadata.Asset;
			var copyAsset = Object.Instantiate(originalAsset);
			var copyMetadata = new AssetMetadata(
				metadata.Id,
				metadata.ContainerId,
				asset: copyAsset,
				metadata.ColliderGeometry,
				source: null);
			Assets[metadata.Id] = copyMetadata;

			IEnumerable<AssetMetadata> dependents;
			if (originalAsset is UnityEngine.Texture tex)
			{
				// identify materials that use this texture
				dependents = Assets.Values.Where(a =>
				{
					if (a.Asset is UnityEngine.Material mat)
					{
						return MREAPI.AppsAPI.MaterialPatcher.UsesTexture(App, mat, tex);
					}
					else return false;
				});
			}
			else if (originalAsset is UnityEngine.Material mat)
			{
				// update material's texture reference to the new copy
				if (dependency != null && updatedDependency != null)
				{
					var matDef = MREAPI.AppsAPI.MaterialPatcher.GeneratePatch(App, mat);
					var updatePatch = new Material();
					if (matDef.MainTextureId == dependency.Value.Id)
					{
						updatePatch.MainTextureId = dependency.Value.Id;
					}
					if (matDef.EmissiveTextureId == dependency.Value.Id)
					{
						updatePatch.EmissiveTextureId = dependency.Value.Id;
					}
					MREAPI.AppsAPI.MaterialPatcher.ApplyMaterialPatch(App, (UnityEngine.Material)copyAsset, updatePatch);
				}

				// update actors that use this material
				AssetReferenceChanged?.Invoke(metadata.Id);

				// identify prefabs that reference this material
				dependents = Assets.Values.Where(a =>
				{
					if (a.Asset is GameObject prefab)
					{
						var renderers = prefab.GetComponentsInChildren<Renderer>();
						return renderers.Any(r => r.sharedMaterial == mat);
					}
					else return false;
				});
			}
			else if (copyAsset is GameObject prefab)
			{
				// copy prefab into local cache
				prefab.transform.SetParent(CacheRootGO().transform, false);

				// update materials
				if (dependency != null && updatedDependency != null)
				{
					var renderers = prefab.GetComponentsInChildren<Renderer>();
					foreach (var r in renderers)
					{
						if (r.sharedMaterial == (UnityEngine.Material)dependency.Value.Asset)
						{
							r.sharedMaterial = (UnityEngine.Material)updatedDependency.Value.Asset;
						}
					}
				}

				dependents = new AssetMetadata[0];
			}
			else
			{
				dependents = new AssetMetadata[0];
			}

			// update dependents
			foreach (var dependent in dependents)
			{
				MakeWriteSafe(dependent, metadata, copyMetadata);
			}

			// return original assets to cache
			MREAPI.AppsAPI.AssetCache.StoreAssets(metadata.Source.ParsedUri, new Object[] { originalAsset }, metadata.Source.Version);
		}
	}
}
