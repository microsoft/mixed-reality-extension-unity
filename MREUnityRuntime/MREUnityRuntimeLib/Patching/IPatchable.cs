// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using Newtonsoft.Json.Linq;

namespace MixedRealityExtension.Patching
{
	internal interface IPatchable
	{
		/// <summary>
		/// Returns whether this patch has any non-null properties
		/// </summary>
		/// <returns></returns>
		bool IsPatched();

		/// <summary>
		/// Write a serialized patch part into the specified path
		/// </summary>
		/// <param name="path"></param>
		/// <param name="value"></param>
		/// <param name="depth"></param>
		void WriteToPath(TargetPath path, JToken value, int depth);

		/// <summary>
		/// Serialize a part of a patch
		/// </summary>
		/// <param name="path"></param>
		/// <param name="value"></param>
		/// <param name="depth"></param>
		/// <returns></returns>
		bool ReadFromPath(TargetPath path, ref JToken value, int depth);

		/// <summary>
		/// Reset all patchable properties to null
		/// </summary>
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
