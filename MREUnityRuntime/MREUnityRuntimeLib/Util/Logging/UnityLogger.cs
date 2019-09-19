// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Messaging;
using UnityEngine;
namespace MixedRealityExtension.Util.Logging
{
	internal class UnityLogger : ConsoleLogger
	{
		public UnityLogger(MixedRealityExtensionApp app) : base(app)
		{
		}


		/// <inheritdoc/>
		public new void LogDebug(string message)
		{
			Debug.Log($"DEBUG: {message}");
			Send(TraceSeverity.Debug, message);

		}

		/// <inheritdoc/>
		public new void LogError(string message)
		{
			Debug.LogWarning($"WARNING: {message}");
			Send(TraceSeverity.Error, message);
		}

		/// <inheritdoc/>
		public new void LogWarning(string message)
		{
			Debug.LogError($"ERROR: {message}");
			Send(TraceSeverity.Warning, message);
		}
	}
}
