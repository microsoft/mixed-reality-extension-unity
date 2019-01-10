// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Util.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using MixedRealityExtension.Messaging;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Util;
using UnityEngine;
using UnityEngine.Networking;
using UnityGLTF;
using UnityGLTF.Loader;
using Object = UnityEngine.Object;

namespace MixedRealityExtension.Assets
{
    using LoaderFunction = Func<AssetSource, Task<IList<Asset>>>;

    /// <summary>
    /// Callback delegate for handling when actors have been successfully created in the engine.
    /// </summary>
    /// <param name="createdActors">The list of actors that were created. createdActors[0] is the root. Null with errors.</param>
    /// <param name="failureMessage">If an error occurs during load, this message is what failed. Null otherwise.</param>
    internal delegate void OnCreatedActorsHandler(IList<Actor> createdActors, string failureMessage);

    internal class AssetLoader : ICommandHandlerContext
    {
        private readonly MonoBehaviour _owner;
        private readonly MixedRealityExtensionApp _app;
        private readonly AsyncCoroutineHelper _asyncHelper;
        private readonly GameObject emptyTemplate = new GameObject();

        internal AssetLoader(MonoBehaviour owner, MixedRealityExtensionApp app)
        {
            _owner = owner ?? throw new ArgumentException("Asset loader requires an owner MonoBehaviour script to be assigned to it.");
            _app = app ?? throw new ArgumentException("Asset loader requires a MixedRealityExtensionApp to be associated with.");
            _asyncHelper = _owner.gameObject.GetComponent<AsyncCoroutineHelper>() ??
                           _owner.gameObject.AddComponent<AsyncCoroutineHelper>();
            emptyTemplate.transform.SetParent(MREAPI.AppsAPI.AssetCache.CacheRootGO().transform);
        }

        internal GameObject GetGameObjectFromParentId(Guid? parentId)
        {
            var parent = _app.FindActor(parentId ?? Guid.Empty) as Actor;
            return parent?.gameObject ?? _app.SceneRoot;
        }

        /*internal void PostCreatePerObject(List<Actor> createdActors, OnCreatedActorsHandler callback)
        {
            if (rootGO != null)
            {
                rootGO.layer = UnityConstants.ActorLayerIndex;
                callback?.Invoke(createdActors, null);
            }

        }*/

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
            GameObject newGO = GameObject.Instantiate(emptyTemplate, GetGameObjectFromParentId(parentId).transform, false);
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
                failureMessage = FormatException(e);
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

            string FormatException(Exception ex)
            {
                Debug.LogException(ex);
                if (ex is HttpRequestException)
                {
                    return $"HttpRequestException: {ex.Message}";
                }
                else
                {
                    // Unrecognized error types should send a usable stack trace back up to the app,
                    // so the user can report it and we can fix it. But Tasks add a ton of noise to stack
                    // traces. This code filters out everything but the actionable data.
                    var lines = ex?.ToString().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    var error = lines[0];
                    var trace = string.Join("\n", lines.Where(l => l.Contains("MixedRealityExtension") || l.Contains("UnityGLTF")));
                    return $"An unexpected error occurred while loading the glTF. The trace is below:\n{error}\n{trace}";
                }
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
