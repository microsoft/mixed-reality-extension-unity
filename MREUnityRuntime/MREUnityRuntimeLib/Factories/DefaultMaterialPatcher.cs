using MixedRealityExtension.API;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Util.Unity;
using System;
using System.Collections.Generic;
using Material = UnityEngine.Material;
using MWMaterial = MixedRealityExtension.Assets.Material;
using Texture = UnityEngine.Texture;

namespace MixedRealityExtension.Factories
{
    /// <summary>
    /// Default implementation of IMaterialPatcher. Only handles color and mainTexture property updates.
    /// </summary>
    public class DefaultMaterialPatcher : IMaterialPatcher
    {
        protected Dictionary<int, Guid> textureAssignments = new Dictionary<int, Guid>(20);

        /// <inheritdoc />
        public virtual void ApplyMaterialPatch(Material material, MWMaterial patch)
        {
            if (patch.Color != null)
                material.color = material.color.ToMWColor().ApplyPatch(patch.Color).ToColor();

            if (patch.MainTextureId != null)
            {
                var textureId = patch.MainTextureId.Value;
                textureAssignments[material.GetInstanceID()] = textureId;
                if (patch.MainTextureId == Guid.Empty)
                {
                    material.mainTexture = null;
                }
                else
                {
                    MREAPI.AppsAPI.AssetCache.OnCached(textureId, tex =>
                    {
                        if (!material || textureAssignments[material.GetInstanceID()] != textureId) return;
                        material.mainTexture = (Texture)tex;
                    });
                }
            }

            if (patch.MainTextureOffset != null)
                material.mainTextureOffset = material.mainTextureOffset.ToMWVector2().ApplyPatch(patch.MainTextureOffset).ToVector2();
            if (patch.MainTextureScale != null)
                material.mainTextureScale = material.mainTextureScale.ToMWVector2().ApplyPatch(patch.MainTextureScale).ToVector2();
        }

        /// <inheritdoc />
        public virtual MWMaterial GeneratePatch(Material material)
        {
            return new MWMaterial()
            {
                Color = new ColorPatch(material.color),
                MainTextureId = MREAPI.AppsAPI.AssetCache.GetId(material.mainTexture),
                MainTextureOffset = new Vector2Patch(material.mainTextureOffset),
                MainTextureScale = new Vector2Patch(material.mainTextureScale)
            };
        }
    }
}
