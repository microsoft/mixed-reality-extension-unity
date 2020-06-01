// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using Texture = UnityEngine.Texture;
using Material = UnityEngine.Material;
using MWMaterial = MixedRealityExtension.Assets.Material;

namespace MixedRealityExtension.PluginInterfaces
{
	/// <summary>
	/// Responsible for translating between the host's material properties and the API properties
	/// </summary>
	public interface IMaterialPatcher
	{
		/// <summary>
		/// Apply the patch from the app to the material
		/// </summary>
		/// <param name="app">The app context of the material</param>
		/// <param name="material">An instance of the default MRE material provided on load</param>
		/// <param name="patch">The update from the app. Unmodified properties will be null.</param>
		void ApplyMaterialPatch(IMixedRealityExtensionApp app, Material material, MWMaterial patch);

		/// <summary>
		/// Generate an API patch from the Unity material's current state
		/// </summary>
		/// <param name="app">The app context of the material</param>
		/// <param name="material">An instance of the default MRE material provided on load</param>
		/// <returns>A full definition of the given material</returns>
		MWMaterial GeneratePatch(IMixedRealityExtensionApp app, Material material);

		/// <summary>
		/// Reports whether a particular Unity texture is in use by the given material
		/// </summary>
		/// <param name="app">The app context of the material</param>
		/// <param name="material">The material to test</param>
		/// <param name="texture">The texture to test</param>
		/// <returns></returns>
		bool UsesTexture(IMixedRealityExtensionApp app, Material material, Texture texture);
	}
}
