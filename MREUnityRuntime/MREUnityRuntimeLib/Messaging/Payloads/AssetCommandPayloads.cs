// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using MixedRealityExtension.Assets;
using MixedRealityExtension.Core;

namespace MixedRealityExtension.Messaging.Payloads
{
    /// <summary>
    /// App => Engine
    /// Payload instructing the engine to preload the listed asset container.
    /// </summary>
    public class LoadAssets : NetworkCommandPayload
    {
        /// <summary>
        /// The asset container to load.
        /// </summary>
        public AssetSource Source;

        /// <summary>
        /// The type of collider to add to the actor upon creation.
        /// </summary>
        public ColliderType ColliderType;
    }

    /// <summary>
    /// Engine => App
    /// Replies to LoadAssetRequests with the contents of the loaded bundle.
    /// </summary>
    public class AssetsLoaded : NetworkCommandPayload
    {
        /// <summary>
        /// If the load failed, this string contains the reason why.
        /// </summary>
        public string FailureMessage;

        /// <summary>
        /// The loaded assets.
        /// </summary>
        public IEnumerable<Asset> Assets;
    }

    /// <summary>
    /// App => Engine
    /// Instructs the engine to instantiate the prefab with the given ID.
    /// </summary>
    public class CreateFromPrefab : CreateActor
    {
        /// <summary>
        /// The ID of an already-loaded asset
        /// </summary>
        public Guid PrefabId { get; set; }

        /// <summary>
        /// Whether or not to enable colliders on this prefab. Prefab must have been given a collider type when loaded.
        /// </summary>
        public bool EnableColliders { get; set; }
    }
}
