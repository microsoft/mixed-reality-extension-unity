// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using GLTF.Schema;
using UnityGLTF;
using UnityGLTF.Loader;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MixedRealityExtension.PluginInterfaces;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;

public class VertexShadedGltfImporter : GLTFSceneImporter
{
	public const int POLYGON_LIMIT = 100000;
	public const int VERTEX_LIMIT = 200000;

	private Material _templateMaterial;

	public VertexShadedGltfImporter(Material templateMaterial, string gltfFileName, ImportOptions options)
		: base(gltfFileName, options)
	{
		_templateMaterial = templateMaterial;
		IsMultithreaded = true;
	}

	public VertexShadedGltfImporter(Material templateMaterial, GLTFRoot root, Stream stream, ImportOptions options)
		: base (root, stream, options)
	{
		_templateMaterial = templateMaterial;
		IsMultithreaded = true;
	}

	protected override async Task ConstructMaterialImageBuffers(GLTFMaterial def)
	{
		if (def.EmissiveTexture != null)
		{
			var textureId = def.EmissiveTexture.Index;
			await ConstructImageBuffer(textureId.Value, textureId.Id);
		}

		if (def.PbrMetallicRoughness?.BaseColorTexture != null)
		{
			var textureId = def.PbrMetallicRoughness.BaseColorTexture.Index;
			await ConstructImageBuffer(textureId.Value, textureId.Id);
		}
	}

	protected override async Task ConstructMaterial(GLTFMaterial def, int materialIndex)
	{
		var material = UnityEngine.Object.Instantiate(_templateMaterial);

		if (def.PbrMetallicRoughness != null)
		{
			var pbr = def.PbrMetallicRoughness;

			material.color = pbr.BaseColorFactor.ToUnityColorRaw();

			if (pbr.BaseColorTexture != null)
			{
				TextureId textureId = pbr.BaseColorTexture.Index;
				await ConstructTexture(textureId.Value, textureId.Id, false, false);
				material.mainTexture = _assetCache.TextureCache[textureId.Id].Texture;

				var ext = GetTextureTransform(pbr.BaseColorTexture);
				if (ext != null)
				{
					material.mainTextureOffset = new Vector2(ext.Offset.X, 1 - ext.Scale.Y - ext.Offset.Y);
					material.mainTextureScale = new Vector2(ext.Scale.X, ext.Scale.Y);
				}
			}
		}
		else
		{
			IExtension sgExt = null;
			def.Extensions?.TryGetValue(KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME, out sgExt);
			var sg = sgExt as KHR_materials_pbrSpecularGlossinessExtension;
			if (sg != null)
			{
				material.color = sg.DiffuseFactor.ToUnityColorRaw();

				if (sg.DiffuseTexture != null)
				{
					var textureId = sg.DiffuseTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, false, false);
					material.mainTexture = _assetCache.TextureCache[textureId.Id].Texture;

					var ext = GetTextureTransform(sg.DiffuseTexture);
					if (ext != null)
					{
						material.mainTextureOffset = new Vector2(ext.Offset.X, 1 - ext.Scale.Y - ext.Offset.Y);
						material.mainTextureScale = new Vector2(ext.Scale.X, ext.Scale.Y);
					}
				}
			}
		}

		material.SetColor("_EmissiveColor", def.EmissiveFactor.ToUnityColorRaw());
		if (def.EmissiveTexture != null)
		{
			TextureId textureId = def.EmissiveTexture.Index;
			await ConstructTexture(textureId.Value, textureId.Id, false, false);
			material.SetTexture("_EmissiveTex", _assetCache.TextureCache[textureId.Id].Texture);

			var ext = GetTextureTransform(def.EmissiveTexture);
			if (ext != null)
			{
				material.SetTextureOffset("_EmissiveTex", new Vector2(ext.Offset.X, 1 - ext.Scale.Y - ext.Offset.Y));
				material.SetTextureScale("_EmissiveTex", new Vector2(ext.Scale.X, ext.Scale.Y));
			}
		}

		material.SetFloat("_AlphaCutoff", (float)def.AlphaCutoff);
		switch (def.AlphaMode)
		{
			case AlphaMode.OPAQUE:
				material.renderQueue = (int)RenderQueue.Geometry;
				material.SetOverrideTag("RenderMode", "Opaque");
				material.SetInt("_ZWrite", 1);
				material.SetInt("_SrcBlend", (int)BlendMode.One);
				material.SetInt("_DstBlend", (int)BlendMode.Zero);
				material.SetInt("_ShouldCutout", 0);
				break;
			case AlphaMode.MASK:
				material.renderQueue = (int)RenderQueue.AlphaTest;
				material.SetOverrideTag("RenderMode", "TransparentCutout");
				material.SetInt("_ZWrite", 1);
				material.SetInt("_SrcBlend", (int)BlendMode.One);
				material.SetInt("_DstBlend", (int)BlendMode.Zero);
				material.SetInt("_ShouldCutout", 1);
				break;
			case AlphaMode.BLEND:
				material.renderQueue = (int)RenderQueue.Transparent;
				material.SetOverrideTag("RenderMode", "Transparent");
				material.SetInt("_ZWrite", 0);
				material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ShouldCutout", 0);
				break;
		}

		MaterialCacheData materialWrapper = new MaterialCacheData
		{
			UnityMaterial = material,
			UnityMaterialWithVertexColor = material,
			GLTFMaterial = def
		};

		if (materialIndex >= 0)
		{
			_assetCache.MaterialCache[materialIndex] = materialWrapper;
		}
		else
		{
			_defaultLoadedMaterial = materialWrapper;
		}
	}
}

public class VertexShadedGltfImporterFactory : IGLTFImporterFactory
{
	public GLTFSceneImporter CreateImporter(string filename, IDataLoader loader, AsyncCoroutineHelper asyncCoroutineHelper)
	{
		return new VertexShadedGltfImporter(MixedRealityExtension.API.MREAPI.AppsAPI.DefaultMaterial, filename, new ImportOptions()
		{
			DataLoader = loader,
			AsyncCoroutineHelper = asyncCoroutineHelper
		});
	}

	public GLTFSceneImporter CreateImporter(GLTFRoot gltfRoot, IDataLoader loader, AsyncCoroutineHelper helper, Stream stream = null)
	{
		return new VertexShadedGltfImporter(MixedRealityExtension.API.MREAPI.AppsAPI.DefaultMaterial, gltfRoot, stream, new ImportOptions()
		{
			DataLoader = loader,
			AsyncCoroutineHelper = helper
		});
	}
}
