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
    using LoaderCallback = Action<IEnumerable<Asset>, string>;
    using LoaderFunction = Action<AssetSource, Action<IEnumerable<Asset>, string>>;

    /// <summary>
    /// Callback delegate for handling when actors have been successfully created in the engine.
    /// </summary>
    /// <param name="createdActors">The list of actors that were created.</param>
    /// <param name="rootGO">The root unity game object for the created asset.</param>
    internal delegate void OnCreatedActorsHandler(IList<Actor> createdActors, GameObject rootGO);

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

        internal void PostCreatePerObject(GameObject rootGO, List<Actor> createdActors, OnCreatedActorsHandler callback)
        {
            if (rootGO != null)
            {
                rootGO.layer = UnityConstants.ActorLayerIndex;
                callback?.Invoke(createdActors, rootGO);
            }

        }

        internal void CreateFromLibrary(string resourceId, Guid? parentId, OnCreatedActorsHandler callback)
        {
            var factory = MREAPI.AppsAPI.LibraryResourceFactory
                ?? throw new ArgumentException("Cannot spawn resource from non-existent library.");

            factory.CreateFromLibrary(
                resourceId,
                GetGameObjectFromParentId(parentId),
                spawnedGO =>
                {
                    List<Actor> actors = new List<Actor>() { spawnedGO.AddComponent<Actor>() };
                    PostCreatePerObject(spawnedGO, actors, callback);
                }
            );
        }

        internal void CreatePrimitive(PrimitiveDefinition definition, Guid? parentId, bool addCollider, OnCreatedActorsHandler callback)
        {
            var factory = MREAPI.AppsAPI.PrimitiveFactory;
            try
            {
                GameObject newGO = factory.CreatePrimitive(
                    definition, GetGameObjectFromParentId(parentId), addCollider);

                List<Actor> actors = new List<Actor>() { newGO.AddComponent<Actor>() };
                PostCreatePerObject(newGO, actors, callback);

            }
            catch (Exception e)
            {
                MREAPI.Logger.LogError($"Failed to create primitive.  Exception: {e.Message}\nStack Trace: {e.StackTrace}");
            }

        }

        internal void CreateEmpty(Guid? parentId, OnCreatedActorsHandler callback)
        {
            GameObject newGO = new GameObject();
            newGO.transform.SetParent(GetGameObjectFromParentId(parentId).transform, false);

            List<Actor> actors = new List<Actor>() { newGO.AddComponent<Actor>() };
            PostCreatePerObject(newGO, actors, callback);
        }

        internal void CreateFromGLTF(string resourceUrl, string assetName, Guid? parentId, ColliderType colliderType, OnCreatedActorsHandler callback)
        {
            GameObject rootGO = null;
            IList<Actor> actors = new List<Actor>();

            UtilMethods.GetUrlParts(resourceUrl, out string rootUrl, out string filename);
            var loader = new WebRequestLoader(rootUrl, _asyncHelper);
            var importer = MREAPI.AppsAPI.GLTFImporterFactory.CreateImporter(filename, loader, _asyncHelper);

            var parent = _app.FindActor(parentId ?? Guid.Empty) as Actor;
            importer.SceneParent = parent?.transform ?? _app.SceneRoot.transform;

            importer.Collider = colliderType.ToGLTFColliderType();

            importer.LoadSceneAsync(-1, rootObject =>
            {
                if (rootObject != null)
                {
                    rootGO = rootObject;

                    MWGOTreeWalker.VisitTree(rootGO, (go) =>
                    {
                        // Set layer index
                        go.layer = UnityConstants.ActorLayerIndex;

                        // Wrap as an actor and clear parent if the object is the scene root.
                        var engineActor = go.AddComponent<Actor>();
                        actors.Add(engineActor);
                    });

                    importer.Dispose();
                }

                callback?.Invoke(actors, rootGO);
            });
        }

        internal void CreateFromPrefab(Guid prefabId, Guid? parentId, OnCreatedActorsHandler callback)
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

            callback?.Invoke(actorList, instance);
        }

        private IEnumerator DownloadFile(string url)
        {
            // TODO: file caching
            using (UnityWebRequest uwr = UnityWebRequest.Get(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.isNetworkError || uwr.isHttpError)
                {
                    yield return $"{uwr.method} {uwr.url} - {uwr.responseCode} {uwr.error}";
                }
                else
                {
                    var bytes = uwr.downloadHandler.data;
                    yield return new MemoryStream(bytes, 0, bytes.Length, false, true);
                }
            }
        }

        [CommandHandler(typeof(LoadAssets))]
        private void LoadAssets(LoadAssets payload)
        {
            LoaderFunction loader;

            switch (payload.Source.ContainerType)
            {
                case AssetContainerType.Gltf:
                    loader = LoadAssetsFromGltf;
                    break;
                default:
                    throw new Exception(
                        $"Cannot load assets from unknown container type {payload.Source.ContainerType.ToString()}");
            }

            try
            {
                loader(payload.Source, Continue);
            }
            catch(Exception e)
            {
                Continue(null, e.Message);
            }

            void Continue(IEnumerable<Asset> assets, string failureMessage)
            {
                _app.Protocol.Send(new Message()
                {
                    ReplyToId = payload.MessageId,
                    Payload = new AssetsLoaded()
                    {
                        FailureMessage = failureMessage,
                        Assets = assets
                    }
                });
            }
        }

        private async void LoadAssetsFromGltf(AssetSource source, LoaderCallback callback)
        {
            IList<Asset> assets = new List<Asset>();
            DeterministicGuids guidGenerator = new DeterministicGuids(UtilMethods.StringToGuid(source.Uri.AbsoluteUri));

            // download file
            UtilMethods.GetUrlParts(source.Uri.AbsoluteUri, out string rootUrl, out string filename);
            var loader = new WebRequestLoader(rootUrl, _asyncHelper);
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
            callback?.Invoke(assets, null);
        }
    }
}
