// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Messaging;
using System;

namespace MixedRealityExtension.Util.Logging
{
	internal class ConsoleLogger : IMRELogger
	{
		MixedRealityExtensionApp _app;
		Traces traces;

		public ConsoleLogger(MixedRealityExtensionApp app)
		{
			_app = app;
			traces = new Traces();
			traces.AddTrace(new Trace() { Severity = TraceSeverity.Info, Message = null });
		}

		protected void Send(TraceSeverity severity, string message)
		{
			traces.Traces[0].Message = message;
			traces.Traces[0].Severity = severity;
			_app?.Protocol.Send(traces);
		}

		/// <inheritdoc/>
		public void LogDebug(string message)
		{
			Console.WriteLine($"DEBUG: {message}");
			Send(TraceSeverity.Debug, message);
		}

		/// <inheritdoc/>
		public void LogError(string message)
		{
			Console.Error.WriteLine($"ERROR: {message}");
			Send(TraceSeverity.Error, message);
		}

		/// <inheritdoc/>
		public void LogWarning(string message)
		{
			Console.WriteLine($"WARNING: {message}");
			Send(TraceSeverity.Warning, message);
		}

	}
}
