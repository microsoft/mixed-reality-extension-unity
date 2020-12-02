// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEngine;

namespace MixedRealityExtension.PluginInterfaces
{
	/// <summary>
	/// A factory class that creates actors from host library resources
	/// </summary>
	public interface ILibraryResourceFactory
	{
		/// <summary>
		/// Instantiate a host-defined actor by resource ID. Will throw an ArgumentException if the resourceId is not recognized.
		/// </summary>
		/// <param name="resourceId">A string that uniquely identifies a library resource to the host app</param>
		/// <param name="parent">The Unity GameObject to attach the library object to</param>
		/// <returns>An async task that will resolve with the spawned GameObject</returns>
		Task<GameObject> CreateFromLibrary(string resourceId, GameObject parent);
	}
}
