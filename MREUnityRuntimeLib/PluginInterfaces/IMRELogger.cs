// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace MixedRealityExtension.PluginInterfaces
{
	/// <summary>
	/// Interface that represents a logger for the MRE SDK.
	/// </summary>
	public interface IMRELogger
	{
		/// <summary>
		/// Log a debug message.
		/// </summary>
		/// <param name="message">The debug message.</param>
		void LogDebug(string message);

		/// <summary>
		/// Log a warning.
		/// </summary>
		/// <param name="message">The warning message.</param>
		void LogWarning(string message);

		/// <summary>
		/// Log an error.
		/// </summary>
		/// <param name="message">The error message.</param>
		void LogError(string message);
	}
}
