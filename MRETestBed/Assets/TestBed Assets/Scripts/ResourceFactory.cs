// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEngine;
using MixedRealityExtension.PluginInterfaces;

public class ResourceFactory : ILibraryResourceFactory
{
	public Task<GameObject> CreateFromLibrary(string resourceId, GameObject parent)
	{
		var prefab = Resources.Load<GameObject>($"Library/{resourceId}");
		if (prefab == null)
		{
			return Task.FromException<GameObject>(new ArgumentException($"Resource with ID {resourceId} not found"));
		}

		return Task.FromResult<GameObject>(GameObject.Instantiate(prefab, parent?.transform, false));
	}
}
