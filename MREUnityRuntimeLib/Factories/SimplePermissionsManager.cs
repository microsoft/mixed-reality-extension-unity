// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core;
using MixedRealityExtension.PluginInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MixedRealityExtension.Factories
{
	/// <summary>
	/// Simple permission manager that grants a fixed set of permissions to all MREs
	/// </summary>
	public class SimplePermissionManager : IPermissionManager
	{
		/// <summary>
		/// The static set of permissions that this manager grants
		/// </summary>
		public Permissions GrantedPermissions { get; private set; }

		/// <summary>
		/// Set up the simple manager
		/// </summary>
		/// <param name="grantedPermissions">The permissions to grant to all MREs</param>
		public SimplePermissionManager(Permissions grantedPermissions)
		{
			GrantedPermissions = grantedPermissions;
		}

		/// <inheritdoc/>
		public event Action<Uri, Permissions, Permissions> OnPermissionDecisionsChanged;

		/// <inheritdoc/>
		public Task<Permissions> PromptForPermissions(
			Uri appLocation,
			IEnumerable<Permissions> permissionsNeeded,
			IEnumerable<Permissions> permissionsWanted,
			Permissions permissionFlagsNeeded,
			Permissions permissionFlagsWanted,
			AppManifest appManifest,
			CancellationToken cancellationToken)
		{
			return Task.FromResult(GrantedPermissions);
		}

		/// <inheritdoc/>
		public Permissions CurrentPermissions(Uri appLocation)
		{
			return GrantedPermissions;
		}
	}
}
