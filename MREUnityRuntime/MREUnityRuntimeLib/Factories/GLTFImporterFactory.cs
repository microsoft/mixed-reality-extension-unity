// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using GLTF.Schema;
using MixedRealityExtension.Assets;
using MixedRealityExtension.PluginInterfaces;
using UnityGLTF;
using UnityGLTF.Loader;

namespace MixedRealityExtension.Factories
{
	/// <inheritdoc cref="IGLTFImporterFactory"/>
	internal class GLTFImporterFactory : IGLTFImporterFactory
	{
		/// <inheritdoc cref="CreateImporter(string, ILoader, AsyncCoroutineHelper)"/>
		public GLTFSceneImporter CreateImporter(
			string gltfFileName,
			ILoader externalDataLoader,
			AsyncCoroutineHelper asyncCoroutineHelper)
		{
			return new GLTFSceneImporter(gltfFileName, new ImportOptions()
			{
				ExternalDataLoader = externalDataLoader,
				AsyncCoroutineHelper = asyncCoroutineHelper
			});
		}

		public GLTFSceneImporter CreateImporter(
			GLTFRoot rootNode,
			ILoader externalDataLoader,
			AsyncCoroutineHelper asyncCoroutineHelper,
			Stream gltfStream = null)
		{
			return new GLTFSceneImporter(rootNode, gltfStream, new ImportOptions()
			{
				ExternalDataLoader = externalDataLoader,
				AsyncCoroutineHelper = asyncCoroutineHelper
			});
		}
	}
}
