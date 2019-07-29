using MixedRealityExtension.API;
using MixedRealityExtension.Core.Types;
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

        private MWColor _materialColor = new MWColor();
        private MWVector2 _textureOffset = new MWVector2();
        private MWVector2 _textureScale = new MWVector2();

        /// <inheritdoc />
        public virtual void ApplyMaterialPatch(Material material, MWMaterial patch)
        {
            if (patch.Color != null)
            {
                _materialColor.FromUnityColor(material.color);
                _materialColor.ApplyPatch(patch.Color);
                material.color = _materialColor.ToColor();
            }

            if (patch.MainTextureOffset != null)
            {
                _textureOffset.FromUnityVector2(material.mainTextureOffset);
                _textureOffset.ApplyPatch(patch.MainTextureOffset);
                material.mainTextureOffset = _textureOffset.ToVector2();
            }

            if (patch.MainTextureScale != null)
            {
                _textureScale.FromUnityVector2(material.mainTextureScale);
                _textureScale.ApplyPatch(patch.MainTextureScale);
                material.mainTextureScale = _textureScale.ToVector2();
            }

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
