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
		/// <inheritdoc cref="CreateImporter(string, IDataLoader, AsyncCoroutineHelper)"/>
		public GLTFSceneImporter CreateImporter(
			string gltfFileName,
			IDataLoader dataLoader,
			AsyncCoroutineHelper asyncCoroutineHelper)
		{
			return new GLTFSceneImporter(gltfFileName, new ImportOptions()
			{
				DataLoader = dataLoader,
				AsyncCoroutineHelper = asyncCoroutineHelper
			});
		}

		/// <inheritdoc cref="CreateImporter(GLTFRoot, IDataLoader, AsyncCoroutineHelper, Stream)"/>
		public GLTFSceneImporter CreateImporter(
			GLTFRoot rootNode,
			IDataLoader dataLoader,
			AsyncCoroutineHelper asyncCoroutineHelper,
			Stream gltfStream = null)
		{
			return new GLTFSceneImporter(rootNode, gltfStream, new ImportOptions()
			{
				DataLoader = dataLoader,
				AsyncCoroutineHelper = asyncCoroutineHelper
			});
		}
	}
}
