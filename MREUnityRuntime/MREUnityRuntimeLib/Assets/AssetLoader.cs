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

namespace MixedRealityExtension.Assets
{
    using LoaderFunction = Func<AssetSource, Task<IList<Asset>>>;

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
            MWGOTreeWalker.VisitTree(instance, go => actorList.Add(go.AddComponent<Actor>()));

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
                assets = await loader(payload.Source);
            }
            catch (Exception e)
            {
                failureMessage = UtilMethods.FormatException(e);
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

        private async Task<IList<Asset>> LoadAssetsFromGLTF(AssetSource source)
        {
            IList<Asset> assets = new List<Asset>();
            DeterministicGuids guidGenerator = new DeterministicGuids(UtilMethods.StringToGuid(source.Uri.AbsoluteUri));

            // download file
            UtilMethods.GetUrlParts(source.Uri.AbsoluteUri, out string rootUrl, out string filename);
            var loader = new WebRequestLoader(rootUrl);
            await loader.LoadStream(filename);

            // pre-parse glTF document so we can get a scene count
            // TODO: run this in thread
            GLTF.GLTFParser.ParseJson(loader.LoadedStream, out GLTF.Schema.GLTFRoot gltfRoot);

            GLTFSceneImporter importer =
                MREAPI.AppsAPI.GLTFImporterFactory.CreateImporter(gltfRoot, loader, _asyncHelper, loader.LoadedStream);
            importer.SceneParent = MREAPI.AppsAPI.AssetCache.CacheRootGO().transform;

            await importer.LoadSceneAsync();

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
                Name = gltfRoot.GetDefaultScene().Name,
                Source = source,
                Prefab = new Prefab
                {
                    ActorCount = actorCount
                }
            };
            MREAPI.AppsAPI.AssetCache.CacheAsset(source, def.Id, rootObject);
            assets.Add(def);

            importer.Dispose();

            return assets;
        }
    }
}
