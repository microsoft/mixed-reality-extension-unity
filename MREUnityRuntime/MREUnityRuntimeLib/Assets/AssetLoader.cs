// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Messaging;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util;
using MixedRealityExtension.Util.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Loader;
using MWMaterial = MixedRealityExtension.Assets.Material;
using MWTexture = MixedRealityExtension.Assets.Texture;
using MWMesh = MixedRealityExtension.Assets.Mesh;
using MWSound = MixedRealityExtension.Assets.Sound;
using MWVideoStream = MixedRealityExtension.Assets.VideoStream;

namespace MixedRealityExtension.Assets
{
	using LoaderFunction = Func<AssetSource, Guid, ColliderType, Task<IList<Asset>>>;

	internal class AssetLoader : ICommandHandlerContext
	{
		private readonly MonoBehaviour _owner;
		private readonly MixedRealityExtensionApp _app;
		private readonly AsyncCoroutineHelper _asyncHelper;

		public readonly HashSet<Guid> ActiveContainers = new HashSet<Guid>();

		internal AssetLoader(MonoBehaviour owner, MixedRealityExtensionApp app)
		{
			_owner = owner ?? throw new ArgumentException("Asset loader requires an owner MonoBehaviour script to be assigned to it.");
			_app = app ?? throw new ArgumentException("Asset loader requires a MixedRealityExtensionApp to be associated with.");
			_asyncHelper = _owner.gameObject.GetComponent<AsyncCoroutineHelper>() ??
						   _owner.gameObject.AddComponent<AsyncCoroutineHelper>();
		}

		internal GameObject GetGameObjectFromParentId(Guid? parentId)
		{
			var parent = _app.FindActor(parentId ?? Guid.Empty) as Actor;
			return parent?.gameObject ?? _app.SceneRoot;
		}

		internal async Task<IList<Actor>> CreateFromLibrary(string resourceId, Guid? parentId)
		{
			var factory = MREAPI.AppsAPI.LibraryResourceFactory
				?? throw new ArgumentException("Cannot spawn resource from non-existent library.");

			var spawnedGO = await factory.CreateFromLibrary(resourceId, GetGameObjectFromParentId(parentId));
			spawnedGO.layer = MREAPI.AppsAPI.LayerApplicator.DefaultLayer;
			return new List<Actor>() { spawnedGO.AddComponent<Actor>() };
		}

		internal IList<Actor> CreateEmpty(Guid? parentId)
		{
			GameObject newGO = GameObject.Instantiate(
				_app.AssetCache.EmptyTemplate(),
				GetGameObjectFromParentId(parentId).transform,
				false);
			newGO.layer = MREAPI.AppsAPI.LayerApplicator.DefaultLayer;

			return new List<Actor>() { newGO.AddComponent<Actor>() };
		}

		internal IList<Actor> CreateFromPrefab(Guid prefabId, Guid? parentId, CollisionLayer? collisionLayer)
		{
			GameObject prefab = _app.AssetCache.GetAsset(prefabId) as GameObject;

			GameObject instance = UnityEngine.Object.Instantiate(
				prefab, GetGameObjectFromParentId(parentId).transform, false);

			// copy animation target mapping
			var sourceMap = prefab.GetComponent<PrefabAnimationTargets>();
			var destMap = instance.GetComponent<PrefabAnimationTargets>();
			if (sourceMap != null && destMap != null)
			{
				destMap.AnimationTargets = sourceMap.AnimationTargets;
			}

			// note: actor properties are set in App#ProcessCreatedActors
			var actorList = new List<Actor>();
			MWGOTreeWalker.VisitTree(instance, go =>
			{
				var collider = go.GetComponent<UnityEngine.Collider>();
				if (collider != null)
				{
					MREAPI.AppsAPI.LayerApplicator.ApplyLayerToCollider(collisionLayer, collider);
				}
				else
				{
					go.layer = MREAPI.AppsAPI.LayerApplicator.DefaultLayer;
				}

				actorList.Add(go.AddComponent<Actor>());
			});

			return actorList;
		}

		[CommandHandler(typeof(LoadAssets))]
		private async Task LoadAssets(LoadAssets payload, Action onCompleteCallback)
		{
			LoaderFunction loader;

			switch (payload.Source.ContainerType)
			{
				case AssetContainerType.GLTF:
					loader = LoadAssetsFromGLTF;
					break;
				default:
					throw new Exception(
						$"Cannot load assets from unknown container type {payload.Source.ContainerType.ToString()}");
			}

			IList<Asset> assets = null;
			string failureMessage = null;

			// attempt to get cached assets instead of loading
			try
			{
				assets = await loader(payload.Source, payload.ContainerId, payload.ColliderType);
				ActiveContainers.Add(payload.ContainerId);
			}
			catch (Exception e)
			{
				failureMessage = UtilMethods.FormatException(
					$"An unexpected error occurred while loading the asset [{payload.Source.Uri}].", e);
			}

			_app.Protocol.Send(new Message()
			{
				ReplyToId = payload.MessageId,
				Payload = new AssetsLoaded()
				{
					FailureMessage = failureMessage,
					Assets = assets ?? new Asset[] { }
				}
			});
			onCompleteCallback?.Invoke();
		}

		private async Task<IList<Asset>> LoadAssetsFromGLTF(AssetSource source, Guid containerId, ColliderType colliderType)
		{
			WebRequestLoader loader = null;
			Stream stream = null;
			IList<Asset> assets = new List<Asset>();
			DeterministicGuids guidGenerator = new DeterministicGuids(UtilMethods.StringToGuid(
				$"{containerId}:{source.ParsedUri.AbsoluteUri}"));

			// Wait asynchronously until the load throttler lets us through.
			using (var scope = await AssetLoadThrottling.AcquireLoadScope())
			{
				// download file
				var rootUrl = URIHelper.GetDirectoryName(source.ParsedUri.AbsoluteUri);
				loader = new WebRequestLoader(rootUrl);
				stream = await loader.LoadStreamAsync(URIHelper.GetFileFromUri(source.ParsedUri));
			}

			// pre-parse glTF document so we can get a scene count
			// run this on a threadpool thread so that the Unity main thread is not blocked
			GLTF.Schema.GLTFRoot gltfRoot = null;
			try
			{
				await Task.Run(() =>
				{
					GLTF.GLTFParser.ParseJson(stream, out gltfRoot);
				});
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}
			if (gltfRoot == null)
			{
				throw new GLTFLoadException("Failed to parse glTF");
			}
			stream.Position = 0;

			using (GLTFSceneImporter importer =
				MREAPI.AppsAPI.GLTFImporterFactory.CreateImporter(gltfRoot, loader, _asyncHelper, stream))
			{
				importer.SceneParent = _app.AssetCache.CacheRootGO().transform;
				importer.Collider = colliderType.ToGLTFColliderType();

				// load textures
				if (gltfRoot.Textures != null)
				{
					for (var i = 0; i < gltfRoot.Textures.Count; i++)
					{
						await importer.LoadTextureAsync(gltfRoot.Textures[i], i, true);
						var texture = importer.GetTexture(i);
						texture.name = gltfRoot.Textures[i].Name ?? $"texture:{i}";

						var asset = GenerateAssetPatch(texture, guidGenerator.Next());
						asset.Name = texture.name;
						asset.Source = new AssetSource(source.ContainerType, source.Uri, $"texture:{i}");
						_app.AssetCache.CacheAsset(texture, asset.Id, containerId, source);
						assets.Add(asset);
					}
				}

				// load meshes
				if (gltfRoot.Meshes != null)
				{
					var cancellationSource = new System.Threading.CancellationTokenSource();
					for (var i = 0; i < gltfRoot.Meshes.Count; i++)
					{
						var mesh = await importer.LoadMeshAsync(i, cancellationSource.Token);
						mesh.name = gltfRoot.Meshes[i].Name ?? $"mesh:{i}";

						var asset = GenerateAssetPatch(mesh, guidGenerator.Next());
						asset.Name = mesh.name;
						asset.Source = new AssetSource(source.ContainerType, source.Uri, $"mesh:{i}");
						var colliderGeo = colliderType == ColliderType.Mesh ?
							(ColliderGeometry)new MeshColliderGeometry() { MeshId = asset.Id } :
							(ColliderGeometry)new BoxColliderGeometry() { Size = (mesh.bounds.size * 0.8f).CreateMWVector3() };
						_app.AssetCache.CacheAsset(mesh, asset.Id, containerId, source, colliderGeo);
						assets.Add(asset);
					}
				}

				// load materials
				if (gltfRoot.Materials != null)
				{
					for (var i = 0; i < gltfRoot.Materials.Count; i++)
					{
						var matdef = gltfRoot.Materials[i];
						var material = await importer.LoadMaterialAsync(i);
						material.name = matdef.Name ?? $"material:{i}";

						var asset = GenerateAssetPatch(material, guidGenerator.Next());
						asset.Name = material.name;
						asset.Source = new AssetSource(source.ContainerType, source.Uri, $"material:{i}");
						_app.AssetCache.CacheAsset(material, asset.Id, containerId, source);
						assets.Add(asset);
					}
				}

				// load prefabs
				if (gltfRoot.Scenes != null)
				{
					for (var i = 0; i < gltfRoot.Scenes.Count; i++)
					{
						await importer.LoadSceneAsync(i).ConfigureAwait(true);

						GameObject rootObject = importer.LastLoadedScene;
						rootObject.name = gltfRoot.Scenes[i].Name ?? $"scene:{i}";

						var animation = rootObject.GetComponent<UnityEngine.Animation>();
						if (animation != null)
						{
							animation.playAutomatically = false;

							// initialize mapping so we know which gameobjects are targeted by which animation clips
							var mapping = rootObject.AddComponent<PrefabAnimationTargets>();
							mapping.Initialize(gltfRoot, i);
						}

						MWGOTreeWalker.VisitTree(rootObject, (go) =>
						{
							go.layer = MREAPI.AppsAPI.LayerApplicator.DefaultLayer;
						});

						var def = GenerateAssetPatch(rootObject, guidGenerator.Next());
						def.Name = rootObject.name;
						def.Source = new AssetSource(source.ContainerType, source.Uri, $"scene:{i}");
						_app.AssetCache.CacheAsset(rootObject, def.Id, containerId, source);
						assets.Add(def);
					}
				}
			}

			return assets;
		}

		[CommandHandler(typeof(AssetUpdate))]
		internal void OnAssetUpdate(AssetUpdate payload, Action onCompleteCallback)
		{
			var def = payload.Asset;
			_app.AssetCache.OnCached(def.Id, asset =>
			{
				if (!_owner) return;

				var mat = asset as UnityEngine.Material;
				var tex = asset as UnityEngine.Texture;
				if (def.Material != null)
				{
					MREAPI.AppsAPI.MaterialPatcher.ApplyMaterialPatch(_app, mat, def.Material.Value);
				}
				else if (def.Texture != null)
				{
					// loading failed
					if (tex == null)
					{
						onCompleteCallback?.Invoke();
						return;
					}

					var texdef = def.Texture.Value;
					if (texdef.WrapModeU != null)
						tex.wrapModeU = texdef.WrapModeU.Value;
					if (texdef.WrapModeV != null)
						tex.wrapModeV = texdef.WrapModeV.Value;
				}
				else if (def.Sound != null)
				{
					// do nothing; sound asset properties are immutable
				}
				else if (def.VideoStream != null)
				{
					// do nothing; sound asset properties are immutable
				}
				else if (def.Mesh != null)
				{
					// do nothing; mesh properties are immutable
				}
				else if (def.AnimationData != null)
				{
					// do nothing; animation data are immutable
				}
				else
				{
					_app.Logger.LogError($"Asset {def.Id} is not patchable, or not of the right type!");
				}
				onCompleteCallback?.Invoke();
			});
		}

		[CommandHandler(typeof(CreateAsset))]
		internal async void OnCreateAsset(CreateAsset payload, Action onCompleteCallback)
		{
			var def = payload.Definition;
			var response = new AssetsLoaded();
			var unityAsset = _app.AssetCache.GetAsset(def.Id);
			ColliderGeometry colliderGeo = null;

			ActiveContainers.Add(payload.ContainerId);

			// create materials
			if (unityAsset == null && def.Material != null)
			{
				unityAsset = UnityEngine.Object.Instantiate(MREAPI.AppsAPI.DefaultMaterial);
			}

			// create textures
			else if (unityAsset == null && def.Texture != null)
			{
				var result = await AssetFetcher<UnityEngine.Texture>.LoadTask(_owner, new Uri(def.Texture.Value.Uri));
				unityAsset = result.Asset;
				if (result.FailureMessage != null)
				{
					response.FailureMessage = result.FailureMessage;
				}
			}

			// create meshes
			else if (unityAsset == null && def.Mesh != null)
			{
				if (def.Mesh.Value.PrimitiveDefinition != null)
				{
					var factory = MREAPI.AppsAPI.PrimitiveFactory;
					try
					{
						unityAsset = factory.CreatePrimitive(def.Mesh.Value.PrimitiveDefinition.Value);
						colliderGeo = ConvertPrimToCollider(def.Mesh.Value.PrimitiveDefinition.Value, def.Id);
					}
					catch (Exception e)
					{
						response.FailureMessage = e.Message;
						MREAPI.Logger.LogError(response.FailureMessage);
					}
				}
				else
				{
					response.FailureMessage = $"Cannot create mesh {def.Id} without a primitive definition";
				}
			}

			// create sounds
			else if (unityAsset == null && def.Sound != null)
			{
				var result = await AssetFetcher<UnityEngine.AudioClip>.LoadTask(_owner, new Uri(def.Sound.Value.Uri));
				unityAsset = result.Asset;
				if (result.FailureMessage != null)
				{
					response.FailureMessage = result.FailureMessage;
				}
			}

			// create video streams
			else if (unityAsset == null && def.VideoStream != null)
			{
				if (MREAPI.AppsAPI.VideoPlayerFactory != null)
				{
					PluginInterfaces.FetchResult result2 = MREAPI.AppsAPI.VideoPlayerFactory.PreloadVideoAsset(def.VideoStream.Value.Uri);
					unityAsset = result2.Asset;
					if (result2.FailureMessage != null)
					{
						response.FailureMessage = result2.FailureMessage;
					}
				}
				else
				{
					response.FailureMessage = "VideoPlayerFactory not implemented";
				}
			}

			// create animation data
			else if (unityAsset == null && def.AnimationData != null)
			{
				var animDataCache = ScriptableObject.CreateInstance<AnimationDataCached>();
				animDataCache.Tracks = def.AnimationData.Value.Tracks;
				unityAsset = animDataCache;
			}

			_app.AssetCache.CacheAsset(unityAsset, def.Id, payload.ContainerId, colliderGeometry: colliderGeo);

			// verify creation and apply initial patch
			if (unityAsset != null)
			{
				unityAsset.name = def.Name;
				OnAssetUpdate(new AssetUpdate()
				{
					Asset = def
				}, null);

				try
				{
					response.Assets = new Asset[] { GenerateAssetPatch(unityAsset, def.Id) };
				}
				catch (Exception e)
				{
					response.FailureMessage = e.Message;
					_app.Logger.LogError(response.FailureMessage);
				}
			}
			else
			{
				if (response.FailureMessage == null)
				{
					response.FailureMessage = $"Not implemented: CreateAsset of new asset type";
				}
				_app.Logger.LogError(response.FailureMessage);
			}

			_app.Protocol.Send(new Message()
			{
				ReplyToId = payload.MessageId,
				Payload = response
			});

			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(UnloadAssets))]
		internal void UnloadAssets(UnloadAssets payload, Action onCompleteCallback)
		{
			_app.AssetCache.UncacheAssetsAndDestroy(payload.ContainerId);

			ActiveContainers.Remove(payload.ContainerId);

			onCompleteCallback?.Invoke();
		}

		private Asset GenerateAssetPatch(UnityEngine.Object unityAsset, Guid id)
		{
			if (unityAsset is GameObject go)
			{
				int actorCount = 0;
				MWGOTreeWalker.VisitTree(go, _ =>
				{
					actorCount++;
				});

				return new Asset
				{
					Id = id,
					Prefab = new Prefab()
					{
						ActorCount = actorCount
					}
				};
			}
			else if (unityAsset is UnityEngine.Material mat)
			{
				return new Asset()
				{
					Id = id,
					Material = MREAPI.AppsAPI.MaterialPatcher.GeneratePatch(_app, mat)
				};
			}
			else if (unityAsset is UnityEngine.Texture tex)
			{
				return new Asset()
				{
					Id = id,
					Texture = new MWTexture()
					{
						Resolution = new Vector2Patch()
						{
							X = tex.width,
							Y = tex.height
						},
						WrapModeU = tex.wrapModeU,
						WrapModeV = tex.wrapModeV
					}
				};
			}
			else if (unityAsset is UnityEngine.Mesh mesh)
			{
				return new Asset()
				{
					Id = id,
					Mesh = new MWMesh()
					{
						VertexCount = mesh.vertexCount,
						TriangleCount = mesh.triangles.Length / 3,
						BoundingBoxDimensions = new Vector3Patch()
						{
							X = mesh.bounds.size.x,
							Y = mesh.bounds.size.y,
							Z = mesh.bounds.size.z
						},
						BoundingBoxCenter = new Vector3Patch()
						{
							X = mesh.bounds.center.x,
							Y = mesh.bounds.center.y,
							Z = mesh.bounds.center.z
						}
					}
				};
			}
			else if (unityAsset is AudioClip sound)
			{
				return new Asset()
				{
					Id = id,
					Sound = new MWSound()
					{
						Duration = sound.length
					}
				};
			}
			else if (unityAsset is VideoStreamDescription videoStream)
			{
				return new Asset()
				{
					Id = id,
					VideoStream = new MWVideoStream()
					{
						Duration = videoStream.Duration
					}
				};
			}
			else if (unityAsset is AnimationDataCached animData)
			{
				return new Asset()
				{
					Id = id
				};
			}
			else
			{
				throw new Exception($"Asset {id} is not patchable, or not of the right type!");
			}
		}

		internal ColliderGeometry ConvertPrimToCollider(PrimitiveDefinition prim, Guid meshId)
		{
			MWVector3 dims = prim.Dimensions;
			switch (prim.Shape)
			{
				case PrimitiveShape.Sphere:
					return new SphereColliderGeometry()
					{
						Radius = dims.SmallestComponentValue() / 2
					};

				case PrimitiveShape.Box:
					return new BoxColliderGeometry()
					{
						Size = dims ?? new MWVector3(1, 1, 1)
					};

				case PrimitiveShape.Capsule:
					return new CapsuleColliderGeometry()
					{
						Size = dims
					};

				case PrimitiveShape.Cylinder:
					dims = dims ?? new MWVector3(0.2f, 1, 0.2f);
					return new MeshColliderGeometry()
					{
						MeshId = meshId
					};

				case PrimitiveShape.Plane:
					dims = dims ?? new MWVector3(1, 0, 1);
					return new BoxColliderGeometry()
					{
						Size = new MWVector3(Mathf.Max(dims.X, 0.01f), Mathf.Max(dims.Y, 0.01f), Mathf.Max(dims.Z, 0.01f))
					};

				default:
					return null;
			}
		}

		protected IEnumerator WaitUntilEnum(WaitUntil waitUntil)
		{
			yield return waitUntil;
		}

		private static void RunCoroutineSync(IEnumerator streamEnum)
		{
			var stack = new Stack<IEnumerator>();
			stack.Push(streamEnum);
			while (stack.Count > 0)
			{
				var enumerator = stack.Pop();
				if (enumerator.MoveNext())
				{
					stack.Push(enumerator);
					var subEnumerator = enumerator.Current as IEnumerator;
					if (subEnumerator != null)
					{
						stack.Push(subEnumerator);
					}
				}
			}
		}
	}
}
