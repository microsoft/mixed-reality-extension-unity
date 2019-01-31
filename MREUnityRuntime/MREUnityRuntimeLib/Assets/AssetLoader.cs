// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Messaging;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Util;
using MixedRealityExtension.Util.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Loader;
using MWMaterial = MixedRealityExtension.Assets.Material;
using MWTexture = MixedRealityExtension.Assets.Texture;

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

            IList<Actor> actors = new List<Actor>();
            MWGOTreeWalker.VisitTree(importer.LastLoadedScene, (go) =>
            {
                // Set layer index
                go.layer = UnityConstants.ActorLayerIndex;

                // Wrap as an actor and clear parent if the object is the scene root.
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

            var actorList = new List<Actor>();
            MWGOTreeWalker.VisitTree(instance, go =>
            {
                var actor = go.AddComponent<Actor>();
                actorList.Add(actor);

                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    actor.MaterialId = MREAPI.AppsAPI.AssetCache.GetId(renderer.sharedMaterial);
                }
            });

            return actorList;
        }

        [CommandHandler(typeof(LoadAssets))]
        private async Task LoadAssets(LoadAssets payload)
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
            try
            {
                assets = await loader(payload.Source, payload.ColliderType);
            }
            catch (Exception e)
            {
                failureMessage = UtilMethods.FormatException(
                    $"An unexpected error occurred while loading the asset [{payload.Source}].", e);
            }
            finally
            {
                _app.Protocol.Send(new Message()
                {
                    ReplyToId = payload.MessageId,
                    Payload = new AssetsLoaded()
                    {
                        FailureMessage = failureMessage,
                        Assets = assets ?? new Asset[] { }
                    }
                });
            }
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
                    int actorCount = 0;
                    MWGOTreeWalker.VisitTree(rootObject, (go) =>
                    {
                        go.layer = UnityConstants.ActorLayerIndex;
                        actorCount++;
                    });

                    Asset def = new Asset
                    {
                        Id = guidGenerator.Next(),
                        Name = gltfRoot.Scenes[i].Name ?? $"scene:{i}",
                        Source = new AssetSource(source.ContainerType, source.Uri, $"scene:{i}"),
                        Prefab = new Prefab
                        {
                            ActorCount = actorCount
                        }
                    };
                    MREAPI.AppsAPI.AssetCache.CacheAsset(source, def.Id, rootObject);
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
                    var asset = new Asset()
                    {
                        Id = guidGenerator.Next(),
                        Name = gltfRoot.Textures[i].Name ?? $"texture:{i}",
                        Source = new AssetSource(source.ContainerType, source.Uri, $"texture:{i}"),
                        Texture = new MWTexture()
                        {
                            Resolution = new MWVector2(texture.width, texture.height),
                            WrapModeU = texture.wrapModeU,
                            WrapModeV = texture.wrapModeV
                        }
                    };
                    MREAPI.AppsAPI.AssetCache.CacheAsset(source, asset.Id, texture);
                    assets.Add(asset);
                }
            }

            // load materials
            if (gltfRoot.Materials != null)
            {
                for (var i = 0; i < gltfRoot.Materials.Count; i++)
                {
                    var material = await importer.LoadMaterialAsync(i);
                    var asset = new Asset()
                    {
                        Id = guidGenerator.Next(),
                        Name = gltfRoot.Materials[i].Name ?? $"material:{i}",
                        Source = new AssetSource(source.ContainerType, source.Uri, $"material:{i}"),
                        Material = new MWMaterial()
                        {
                            Color = material.color.ToMWColor(),
                            MainTextureId = MREAPI.AppsAPI.AssetCache.GetId(material.mainTexture),
                            MainTextureOffset = material.mainTextureOffset.ToMWVector2(),
                            MainTextureScale = material.mainTextureScale.ToMWVector2()
                        }
                    };
                    MREAPI.AppsAPI.AssetCache.CacheAsset(source, asset.Id, material);
                    assets.Add(asset);
                }
            }

            importer.Dispose();

            return assets;
        }

        public void OnAssetUpdate(Asset def)
        {
            var asset = MREAPI.AppsAPI.AssetCache.GetAsset(def.Id);

            var mat = asset as UnityEngine.Material;
            var tex = asset as UnityEngine.Texture;
            if (def.Material != null && mat != null)
            {
                var matdef = def.Material.Value;
                if (matdef.Color != null)
                    mat.color = matdef.Color.ToColor();
                if (matdef.MainTextureId != null)
                    mat.mainTexture = MREAPI.AppsAPI.AssetCache.GetAsset(matdef.MainTextureId) as UnityEngine.Texture;
                if (matdef.MainTextureOffset != null)
                    mat.mainTextureOffset = matdef.MainTextureOffset.ToVector2();
                if (matdef.MainTextureScale != null)
                    mat.mainTextureScale = matdef.MainTextureScale.ToVector2();
            }
            else if(def.Texture != null && tex != null)
            {
                var texdef = def.Texture.Value;
                if (texdef.WrapModeU != null)
                    tex.wrapModeU = texdef.WrapModeU.Value;
                if (texdef.WrapModeV != null)
                    tex.wrapModeV = texdef.WrapModeV.Value;
            }
            else
            {
                MREAPI.Logger.LogError($"Asset {def.Id} is not patchable, or not of the right type!");
            }
        }
    }
}
