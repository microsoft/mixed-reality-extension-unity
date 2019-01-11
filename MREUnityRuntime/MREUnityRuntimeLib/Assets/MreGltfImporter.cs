using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLTF.Schema;
using MixedRealityExtension.PluginInterfaces;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityGLTF.Loader;

namespace MixedRealityExtension.Assets
{
    public class MreGltfImporter : GLTFSceneImporter
    {
        public MreGltfImporter(string gltfFileName, ILoader externalDataLoader, AsyncCoroutineHelper asyncCoroutineHelper)
            : base(gltfFileName, externalDataLoader, asyncCoroutineHelper)
        { }

        public MreGltfImporter(GLTFRoot rootNode, ILoader externalDataLoader, AsyncCoroutineHelper asyncCoroutineHelper, Stream gltfStream = null)
            : base(rootNode, externalDataLoader, asyncCoroutineHelper, gltfStream)
        { }

        public async Task<Material> LoadMaterialAsync(int materialIndex)
        {
            try
            {
                lock (this)
                {
                    if (_isRunning)
                    {
                        throw new GLTFLoadException("Cannot CreateTexture while GLTFSceneImporter is already running");
                    }

                    _isRunning = true;
                }

                if (_assetCache == null)
                {
                    InitializeAssetCache();
                }

                _timeAtLastYield = Time.realtimeSinceStartup;
                if (_assetCache.MaterialCache[materialIndex] == null)
                {
                    await ConstructMaterial(_gltfRoot.Materials[materialIndex], materialIndex);
                }
                return _assetCache.MaterialCache[materialIndex].UnityMaterialWithVertexColor;
            }
            finally
            {
                lock (this)
                {
                    _isRunning = false;
                }
            }
        }
    }
}
