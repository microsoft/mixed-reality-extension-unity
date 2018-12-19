// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.PluginInterfaces;
using UnityEngine;

namespace MixedRealityExtension.Util.Logging
{
    internal class UnityLogger : IMRELogger
    {
        /// <inheritdoc/>
        public void LogDebug(string message)
        {
            Debug.Log($"DEBUG: {message}");
        }

        /// <inheritdoc/>
        public void LogError(string message)
        {
            Debug.LogWarning($"WARNING: {message}");
        }

        /// <inheritdoc/>
        public void LogWarning(string message)
        {
            Debug.LogError($"ERROR: {message}");
        }
    }
}
