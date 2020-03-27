// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using Newtonsoft.Json.Linq;

namespace MixedRealityExtension.Patching
{
	internal interface IPatchable
	{
		void WriteToPath(TargetPath path, JToken value, int depth);
		void Clear();
		/// <summary>
		/// Assign a saved patch instance to public property
		/// </summary>
		/// <param name="path">The path whose parts should be restored</param>
		/// <param name="depth">Which path part should be restored on this object</param>
		void Restore(TargetPath path, int depth);

		/// <summary>
		/// Assign all saved patches to public properties
		/// </summary>
		void RestoreAll();
	}
}
