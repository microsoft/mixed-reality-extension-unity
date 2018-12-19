// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.PluginInterfaces;
using System;

namespace MixedRealityExtension.Util.Logging
{
    internal class ConsoleLogger : IMRELogger
    {
        /// <inheritdoc/>
        public void LogDebug(string message)
        {
            Console.WriteLine($"DEBUG: {message}");
        }

        /// <inheritdoc/>
        public void LogError(string message)
        {
            Console.Error.WriteLine($"ERROR: {message}");
        }

        /// <inheritdoc/>
        public void LogWarning(string message)
        {
            Console.WriteLine($"WARNING: {message}");
        }
    }
}
