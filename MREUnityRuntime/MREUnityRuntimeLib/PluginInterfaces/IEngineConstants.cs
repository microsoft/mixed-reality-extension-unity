// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace MixedRealityExtension.PluginInterfaces
{
    public struct MRELayers
    {
        // int Object;
        // int Environment;
        // int Hologram;
    }

    public interface IEngineConstants
    {
        MRELayers Layers { get; }
    }
}
