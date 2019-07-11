// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Messaging;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util;
using MixedRealityExtension.Util.Unity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Loader;
using MWMaterial = MixedRealityExtension.Assets.Material;
using MWTexture = MixedRealityExtension.Assets.Texture;
using MWSound = MixedRealityExtension.Assets.Sound;
using MWVideoStream = MixedRealityExtension.Assets.VideoStream;

namespace MixedRealityExtension.Assets
{
    using LoaderFunction = Func<AssetSource, ColliderType, Task<IList<Asset>>>;

    internal class AssetLoader : ICommandHandlerContext
    {
        private readonly MonoBehaviour _owner;
        private readonly MixedRealityExtensionApp _app;
        private readonly AsyncCoroutineHelper _asyncHelper;

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
            spawnedGO.layer = UnityConstants.ActorLayerIndex;
            return new List<Actor>() { spawnedGO.AddComponent<Actor>() };
        }

        internal IList<Actor> CreatePrimitive(PrimitiveDefinition definition, Guid? parentId, bool addCollider)
        {
            var factory = MREAPI.AppsAPI.PrimitiveFactory;
            GameObject newGO = factory.CreatePrimitive(
                definition, GetGameObjectFromParentId(parentId), addCollider);

            List<Actor> actors = new List<Actor>() { newGO.AddComponent<Actor>() };
            newGO.layer = UnityConstants.ActorLayerIndex;
            return actors;
        }

        internal IList<Actor> CreateEmpty(Guid? parentId)
        {
            GameObject newGO = GameObject.Instantiate(
                MREAPI.AppsAPI.AssetCache.EmptyTemplate(),
                GetGameObjectFromParentId(parentId).transform,
                false);
            newGO.layer = UnityConstants.ActorLayerIndex;

            return new List<Actor>() { newGO.AddComponent<Actor>() };
        }

        internal async Task<IList<Actor>> CreateFromGLTF(string resourceUrl, string assetName, Guid? parentId, ColliderType colliderType)
        {
            UtilMethods.GetUrlParts(resourceUrl, out string rootUrl, out string filename);
            var loader = new WebRequestLoader(rootUrl);
            var importer = MREAPI.AppsAPI.GLTFImporterFactory.CreateImporter(filename, loader, _asyncHelper);

            var parent = _app.FindActor(parentId ?? Guid.Empty) as Actor;
            importer.SceneParent = parent?.transform ?? _app.SceneRoot.transform;

            importer.Collider = colliderType.ToGLTFColliderType();

            await importer.LoadSceneAsync().ConfigureAwait(true);

            // note: actor properties are set in App#ProcessCreatedActors
            IList<Actor> actors = new List<Actor>();
            MWGOTreeWalker.VisitTree(importer.LastLoadedScene, (go) =>
            {
                go.layer = UnityConstants.ActorLayerIndex;
                actors.Add(go.AddComponent<Actor>());
            });

            importer.Dispose();

            return actors;
        }

        internal IList<Actor> CreateFromPrefab(Guid prefabId, Guid? parentId)
        {
            GameObject prefab = MREAPI.AppsAPI.AssetCache.GetAsset(prefabId) as GameObject;
            if (prefab == null)
            {
                throw new ArgumentException($"Asset {prefabId} does not exist or is the wrong type.");
            }

            GameObject instance = UnityEngine.Object.Instantiate(
                prefab, GetGameObjectFromParentId(parentId).transform, false);

            // note: actor properties are set in App#ProcessCreatedActors
            var actorList = new List<Actor>();
            MWGOTreeWalker.VisitTree(instance, go =>
            {
                go.layer = UnityConstants.ActorLayerIndex;
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
            var oldGuids = MREAPI.AppsAPI.AssetCache.GetAssetIdsInSource(payload.Source);
            if (oldGuids != null)
            {
                assets = new List<Asset>(5);
                foreach (var guid in oldGuids)
                {
                    var oldAsset = MREAPI.AppsAPI.AssetCache.GetAsset(guid);
                    if (oldAsset == null)
                    {
                        continue;
                    }

                    var patch = GenerateAssetPatch(oldAsset, guid);
                    patch.Name = oldAsset.name;
                    patch.Source = payload.Source;
                    assets.Add(patch);
                }
            }
            else
            {
                try
                {
                    assets = await loader(payload.Source, payload.ColliderType);
                }
                catch (Exception e)
                {
                    failureMessage = UtilMethods.FormatException(
                        $"An unexpected error occurred while loading the asset [{payload.Source.Uri}].", e);
                }
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

        private async Task<IList<Asset>> LoadAssetsFromGLTF(AssetSource source, ColliderType colliderType)
        {
            IList<Asset> assets = new List<Asset>();
            DeterministicGuids guidGenerator = new DeterministicGuids(UtilMethods.StringToGuid(source.ParsedUri.AbsoluteUri));

            // download file
            UtilMethods.GetUrlParts(source.ParsedUri.AbsoluteUri, out string rootUrl, out string filename);
            var loader = new WebRequestLoader(rootUrl);
            await loader.LoadStream(filename);

            // pre-parse glTF document so we can get a scene count
            // TODO: run this in thread
            GLTF.GLTFParser.ParseJson(loader.LoadedStream, out GLTF.Schema.GLTFRoot gltfRoot);

            GLTFSceneImporter importer =
                MREAPI.AppsAPI.GLTFImporterFactory.CreateImporter(gltfRoot, loader, _asyncHelper, loader.LoadedStream);
            importer.SceneParent = MREAPI.AppsAPI.AssetCache.CacheRootGO().transform;
            importer.Collider = colliderType.ToGLTFColliderType();

            // load prefabs
            if (gltfRoot.Scenes != null)
            {
                for (var i = 0; i < gltfRoot.Scenes.Count; i++)
                {
                    await importer.LoadSceneAsync(i);

                    GameObject rootObject = importer.LastLoadedScene;
                    rootObject.name = gltfRoot.Scenes[i].Name ?? $"scene:{i}";
                    MWGOTreeWalker.VisitTree(rootObject, (go) =>
                    {
                        go.layer = UnityConstants.ActorLayerIndex;
                    });

                    var def = GenerateAssetPatch(rootObject, guidGenerator.Next());
                    def.Name = rootObject.name;
                    def.Source = new AssetSource(source.ContainerType, source.Uri, $"scene:{i}");
                    MREAPI.AppsAPI.AssetCache.CacheAsset(rootObject, def.Id, source);
                    assets.Add(def);
                }
            }

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
                    MREAPI.AppsAPI.AssetCache.CacheAsset(texture, asset.Id, source);
                    assets.Add(asset);
                }
            }

            // load materials
            if (gltfRoot.Materials != null)
            {
                for (var i = 0; i < gltfRoot.Materials.Count; i++)
                {
                    var material = await importer.LoadMaterialAsync(i);
                    material.name = gltfRoot.Materials[i].Name ?? $"material:{i}";

                    var asset = GenerateAssetPatch(material, guidGenerator.Next());
                    asset.Name = material.name;
                    asset.Source = new AssetSource(source.ContainerType, source.Uri, $"material:{i}");
                    MREAPI.AppsAPI.AssetCache.CacheAsset(material, asset.Id, source);
                    assets.Add(asset);
                }
            }

            importer.Dispose();

            return assets;
        }

        [CommandHandler(typeof(AssetUpdate))]
        internal void OnAssetUpdate(AssetUpdate payload, Action onCompleteCallback)
        {
            var def = payload.Asset;
            var asset = MREAPI.AppsAPI.AssetCache.GetAsset(def.Id);

            var mat = asset as UnityEngine.Material;
            var tex = asset as UnityEngine.Texture;
            if (def.Material != null && mat != null)
            {
                var matdef = def.Material.Value;
                if (matdef.MainTextureId != null && matdef.MainTextureId != Guid.Empty)
                {
                    assignOrQueueTexture(def.Id, matdef.MainTextureId.Value);
                    matdef.MainTextureId = null; // so the MaterialPatcher doesn't pick it up
                }
                MREAPI.AppsAPI.MaterialPatcher.ApplyMaterialPatch(mat, matdef);
            }
            else if(def.Texture != null && tex != null)
            {
                var texdef = def.Texture.Value;
                if (texdef.WrapModeU != null)
                    tex.wrapModeU = texdef.WrapModeU.Value;
                if (texdef.WrapModeV != null)
                    tex.wrapModeV = texdef.WrapModeV.Value;
            }
            else if (def.Sound != null && asset as UnityEngine.AudioClip != null)
            {
                // do nothing; sound asset properties are immutable
            }
            else if (def.VideoStream != null && asset as VideoStreamDescription != null)
            {
                // do nothing; sound asset properties are immutable
            }
            else
            {
                MREAPI.Logger.LogError($"Asset {def.Id} is not patchable, or not of the right type!");
            }
            onCompleteCallback?.Invoke();
        }

        [CommandHandler(typeof(CreateAsset))]
        internal async void OnCreateAsset(CreateAsset payload, Action onCompleteCallback)
        {
            var def = payload.Definition;
            var response = new AssetsLoaded();

            var unityAsset = MREAPI.AppsAPI.AssetCache.GetAsset(def.Id);
            if(unityAsset == null && def.Material != null)
            {
                unityAsset = UnityEngine.Object.Instantiate(MREAPI.AppsAPI.DefaultMaterial);
                MREAPI.AppsAPI.AssetCache.CacheAsset(unityAsset, def.Id);
            }
            else if(unityAsset == null && def.Texture != null)
            {
                var result = await AssetFetcher<UnityEngine.Texture>.LoadTask(_owner, new Uri(def.Texture.Value.Uri));
                if(result.FailureMessage != null)
                {
                    response.FailureMessage = result.FailureMessage;
                }
                else
                {
                    unityAsset = result.Asset;
                    MREAPI.AppsAPI.AssetCache.CacheAsset(unityAsset, def.Id);
                    assignTextureToQueuedMaterials(def.Id);
                }
            }
            else if(unityAsset == null && def.Sound != null)
            {
                var result = await AssetFetcher<UnityEngine.AudioClip>.LoadTask(_owner, new Uri(def.Sound.Value.Uri));
                if (result.FailureMessage != null)
                {
                    response.FailureMessage = result.FailureMessage;
                }
                else
                {
                    unityAsset = result.Asset;
                    MREAPI.AppsAPI.AssetCache.CacheAsset(unityAsset, def.Id);
                }
            }
            else if (unityAsset == null && def.VideoStream != null)
            {
                if (MREAPI.AppsAPI.VideoPlayerFactory != null)
                {
                    MixedRealityExtension.PluginInterfaces.FetchResult result2 = MREAPI.AppsAPI.VideoPlayerFactory.PreloadVideoAsset(def.VideoStream.Value.Uri);
                    if (result2.FailureMessage != null)
                    {
                        response.FailureMessage = result2.FailureMessage;
                    }
                    else
                    {
                        unityAsset = result2.Asset;
                        MREAPI.AppsAPI.AssetCache.CacheAsset(unityAsset, def.Id);
                    }
                }
                else
                {
                    response.FailureMessage = "VideoPlayerFactory not implemented";
                }
            }
            if (unityAsset != null)
            {
                OnAssetUpdate(new AssetUpdate()
                {
                    Asset = def
                }, null);

                try
                {
                    response.Assets = new Asset[] { GenerateAssetPatch(unityAsset, def.Id) };
                }
                catch(Exception e)
                {
                    response.FailureMessage = e.Message;
                    MREAPI.Logger.LogError(response.FailureMessage);
                }
            }
            else
            {
                if (response.FailureMessage == null)
                {
                    response.FailureMessage = $"Not implemented: CreateAsset of new asset type";
                }
                MREAPI.Logger.LogError(response.FailureMessage);
            }

            _app.Protocol.Send(new Message()
            {
                ReplyToId = payload.MessageId,
                Payload = response
            });

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
                    Material = MREAPI.AppsAPI.MaterialPatcher.GeneratePatch(mat)
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
            else
            {
                throw new Exception($"Asset {id} is not patchable, or not of the right type!");
            }
        }

        #region Async texture management

        /// <summary>
        /// Maps texture IDs to the list of material IDs assigned to that texture
        /// </summary>
        private readonly Dictionary<Guid, List<Guid>> materialsWaitingForTexture = new Dictionary<Guid, List<Guid>>(5);

        /// <summary>
        /// Maps material IDs to the texture IDs assigned to them
        /// </summary>
        private readonly Dictionary<Guid, Guid> textureOnMaterial = new Dictionary<Guid, Guid>(5);

        /// <summary>
        /// Mark a material to receive a texture once it's loaded
        /// </summary>
        /// <param name="materialId"></param>
        /// <param name="textureId"></param>
        private void assignOrQueueTexture(Guid materialId, Guid textureId)
        {
            // if the material already has a texture on it
            if (textureOnMaterial.TryGetValue(materialId, out var oldTexId)
                // and that texture is also pending
                && materialsWaitingForTexture.ContainsKey(oldTexId)
                && materialsWaitingForTexture[oldTexId].Contains(materialId))
            {
                // dequeue the material from the old tex's update queue
                materialsWaitingForTexture[oldTexId].Remove(materialId);

                // and clean up old tex queue if it's now empty
                if(materialsWaitingForTexture[oldTexId].Count == 0)
                    materialsWaitingForTexture.Remove(oldTexId);
            }

            // assign the texture to the material
            textureOnMaterial[materialId] = textureId;

            var tex = MREAPI.AppsAPI.AssetCache.GetAsset(textureId) as UnityEngine.Texture;
            if (tex == null)
            {
                // new texture isn't loaded yet, queue the material to receive it when it's done
                var waiting = materialsWaitingForTexture.GetOrCreate(textureId, () => new List<Guid>(3));
                waiting.Add(materialId);
            }
            else
            {
                // assign immediately
                var mat = MREAPI.AppsAPI.AssetCache.GetAsset(materialId) as UnityEngine.Material;
                MREAPI.AppsAPI.MaterialPatcher.ApplyMaterialPatch(mat, new MWMaterial() {
                    MainTextureId = textureId
                });
            }
        }

        /// <summary>
        /// Assign the loaded texture to materials waiting for it
        /// </summary>
        /// <param name="textureId"></param>
        private void assignTextureToQueuedMaterials(Guid textureId)
        {
            // skip if no waiting materials
            if (!materialsWaitingForTexture.ContainsKey(textureId))
                return;

            var tex = MREAPI.AppsAPI.AssetCache.GetAsset(textureId) as UnityEngine.Texture;
            foreach (var matId in materialsWaitingForTexture[textureId])
            {
                var mat = MREAPI.AppsAPI.AssetCache.GetAsset(matId) as UnityEngine.Material;
                MREAPI.AppsAPI.MaterialPatcher.ApplyMaterialPatch(mat, new MWMaterial() {
                    MainTextureId = textureId
                });
            }

            materialsWaitingForTexture.Remove(textureId);
        }

        #endregion
    }
}
