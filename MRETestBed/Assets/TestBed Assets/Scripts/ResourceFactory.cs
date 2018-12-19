// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using UnityEngine;
using MixedRealityExtension.PluginInterfaces;

public class ResourceFactory : ILibraryResourceFactory
{
    public void CreateFromLibrary(string resourceId, GameObject parent, Action<GameObject> callback)
    {
        var prefab = Resources.Load<GameObject>($"Library/{resourceId}");
        if (prefab == null)
        {
            var libPlaceholderActor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            libPlaceholderActor.transform.SetParent(parent.transform, false);
            callback(libPlaceholderActor);
            return;
        }

        var libActor = GameObject.Instantiate(prefab, parent?.transform, false);
        callback(libActor);
    }
}
