using MWAssets = MixedRealityExtension.Assets;
using MixedRealityExtension.Core.Types;
using ColorPatch = MixedRealityExtension.Patching.Types.ColorPatch;

using UnityEngine;
using UnityEngine.Rendering;

public class VertexMaterialPatcher : MixedRealityExtension.Factories.DefaultMaterialPatcher
{
	private static int EmissiveProp = Shader.PropertyToID("_Emissive");

	public override void ApplyMaterialPatch(Material material, MWAssets.Material patch)
	{
		base.ApplyMaterialPatch(material, patch);

		if (patch.EmissiveColor != null)
		{
			var color = material.GetColor(EmissiveProp);
			color.r = patch.EmissiveColor.R ?? color.r;
			color.g = patch.EmissiveColor.G ?? color.g;
			color.b = patch.EmissiveColor.B ?? color.b;
			color.a = patch.EmissiveColor.A ?? color.a;
			material.SetColor(EmissiveProp, color);
		}

		if (patch.AlphaCutoff != null)
		{
			material.SetFloat("_AlphaCutoff", patch.AlphaCutoff.Value);
		}

		switch (patch.AlphaMode)
		{
			case MWAssets.AlphaMode.Opaque:
				material.renderQueue = (int)RenderQueue.Geometry;
				material.SetOverrideTag("RenderMode", "Opaque");
				material.SetInt("_ZWrite", 1);
				material.SetInt("_SrcBlend", (int)BlendMode.One);
				material.SetInt("_DstBlend", (int)BlendMode.Zero);
				material.SetInt("_ShouldCutout", 0);
				break;
			case MWAssets.AlphaMode.Mask:
				material.renderQueue = (int)RenderQueue.AlphaTest;
				material.SetOverrideTag("RenderMode", "TransparentCutout");
				material.SetInt("_ZWrite", 1);
				material.SetInt("_SrcBlend", (int)BlendMode.One);
				material.SetInt("_DstBlend", (int)BlendMode.Zero);
				material.SetInt("_ShouldCutout", 1);
				break;
			case MWAssets.AlphaMode.Blend:
				material.renderQueue = (int)RenderQueue.Transparent;
				material.SetOverrideTag("RenderMode", "Transparent");
				material.SetInt("_ZWrite", 0);
				material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ShouldCutout", 0);
				break;
			// ignore default case, i.e. null
		}
	}

	public override MWAssets.Material GeneratePatch(Material material)
	{
		var patch = base.GeneratePatch(material);

		var unityColor = material.GetColor(EmissiveProp);
		patch.EmissiveColor = new ColorPatch()
		{
			R = unityColor.r,
			G = unityColor.g,
			B = unityColor.b,
			A = unityColor.a
		};

		patch.AlphaCutoff = material.GetFloat("_AlphaCutoff");
		patch.AlphaMode =
			material.renderQueue == (int)RenderQueue.Transparent ? MWAssets.AlphaMode.Blend :
			material.renderQueue == (int)RenderQueue.AlphaTest ? MWAssets.AlphaMode.Mask :
			MWAssets.AlphaMode.Opaque;

		return patch;
	}
}
