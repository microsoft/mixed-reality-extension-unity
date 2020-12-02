// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using GLTF.Schema;
using MixedRealityExtension.Assets;
using UnityGLTF;
using UnityGLTF.Loader;

namespace MixedRealityExtension.PluginInterfaces
{
	/// <summary>
	/// Used to generate new GLTFSceneImporter instances. Primarily used for inserting custom subclasses of this type.
	/// </summary>
	public interface IGLTFImporterFactory
	{
		/// <summary>
		/// Returns a new glTF importer to the MRE system. Will typically be a subclass instance, and not a direct instance.
		/// </summary>
		/// <param name="gltfFileName"></param>
		/// <param name="dataLoader"></param>
		/// <param name="asyncCoroutineHelper"></param>
		/// <returns>A new importer instance.</returns>
		GLTFSceneImporter CreateImporter(
			string gltfFileName,
			IDataLoader dataLoader,
			AsyncCoroutineHelper asyncCoroutineHelper);

		/// <summary>
		/// Returns a new glTF importer to the MRE system. Will typically be a subclass instance, and not a direct instance.
		/// </summary>
		/// <param name="rootNode"></param>
		/// <param name="dataLoader"></param>
		/// <param name="asyncCoroutineHelper"></param>
		/// <param name="gltfStream"></param>
		/// <returns></returns>
		GLTFSceneImporter CreateImporter(
			GLTFRoot rootNode,
			IDataLoader dataLoader,
			AsyncCoroutineHelper asyncCoroutineHelper,
			Stream gltfStream = null);
	}
}
