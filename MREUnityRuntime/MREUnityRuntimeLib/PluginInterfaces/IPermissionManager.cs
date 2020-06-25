// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core;
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MixedRealityExtension.PluginInterfaces
{
	/// <summary>
	/// Permission management interface for MRE host apps. Supports both IEnumerable and bitfield-based permission lists.
	/// </summary>
	public interface IPermissionManager
	{
		/// <summary>
		/// Request permissions from the user, and return a Task that resolves with those permissions the user has granted.
		/// </summary>
		/// <param name="appOrigin">The origin of the MRE requesting permission.</param>
		/// <param name="appManifest">The full app manifest, which includes required and optional permissions.</param>
		/// <returns></returns>
		Task<IEnumerable<Permissions>> PromptForPermissions(
			string appOrigin,
			AppManifest appManifest);

		/// <param name="appOrigin">The origin of the MRE that you want to know about.</param>
		/// <returns>A list of the currently granted permissions for the given MRE.</returns>
		IEnumerable<Permissions> CurrentPermissions(string appOrigin);

		/// <summary>
		/// Same as [[CurrentPermissions]], but using bitfields for permissions instead of enumerables.
		/// </summary>
		/// <param name="appOrigin">The origin of the MRE that you want to know about.</param>
		/// <returns>A bitfield of the currently granted permissions for the given MRE.</returns>
		Permissions CurrentPermissionFlags(string appOrigin);
	}
}
