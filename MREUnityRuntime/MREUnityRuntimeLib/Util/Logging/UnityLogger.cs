// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Messaging;
using UnityEngine;
namespace MixedRealityExtension.Util.Logging
{
    internal class UnityLogger : IMRELogger
    {
        MixedRealityExtensionApp _app;
        public UnityLogger(MixedRealityExtensionApp app)
        {
            _app = app;
        }
        void Send(TraceSeverity severity, string message)
        {
            Traces traces = new Traces();
            traces.AddTrace(new Trace() { Severity = severity, Message = message });
            _app?.Protocol.Send(traces);
        }

        /// <inheritdoc/>
        public void LogDebug(string message)
        {
            Debug.Log($"DEBUG: {message}");
            Send(TraceSeverity.Debug, message);

        }

        /// <inheritdoc/>
        public void LogError(string message)
        {
            Debug.LogWarning($"WARNING: {message}");
            Send(TraceSeverity.Error, message);
        }

        /// <inheritdoc/>
        public void LogWarning(string message)
        {
            Debug.LogError($"ERROR: {message}");
            Send(TraceSeverity.Warning, message);
        }
    }
}
