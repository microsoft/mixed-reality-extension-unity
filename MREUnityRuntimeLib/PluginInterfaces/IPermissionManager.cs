// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core;
using System;
using System.Collections.Generic;
using System.Threading;
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
		/// <param name="appLocation">The URI of the MRE requesting permission.</param>
		/// <param name="permissionsNeeded">An enumerable of the permissions required for the MRE to run.</param>
		/// <param name="permissionsWanted">An enumerable of the permissions the MRE can use, but are not required.</param>
		/// <param name="permissionFlagsNeeded">Same as permissionsNeeded, but in a bitfield.</param>
		/// <param name="permissionFlagsWanted">Same as permissionsWanted, but in a bitfield.</param>
		/// <param name="appManifest">The full app manifest, which includes enumerations of the required and optional permissions.</param>
		/// <param name="cancellationToken">Used to cancel the request if it doesn't matter anymore.</param>
		/// <returns></returns>
		Task<Permissions> PromptForPermissions(
			Uri appLocation,
			IEnumerable<Permissions> permissionsNeeded,
			IEnumerable<Permissions> permissionsWanted,
			Permissions permissionFlagsNeeded,
			Permissions permissionFlagsWanted,
			AppManifest appManifest,
			CancellationToken cancellationToken);

		/// <summary>
		/// Get the currently granted permissions for the MRE origin without requesting new ones.
		/// </summary>
		/// <param name="appLocation">The URI of the MRE that you want to know about.</param>
		/// <returns>A bitfield of the currently granted permissions for the given MRE.</returns>
		Permissions CurrentPermissions(Uri appLocation);

		/// <summary>
		/// Event that is fired when any permissions are edited. Receives as arguments the app location URI, the old
		/// permission set, and the new permission set.
		/// </summary>
		event Action<Uri, Permissions, Permissions> OnPermissionDecisionsChanged;
	}
}
