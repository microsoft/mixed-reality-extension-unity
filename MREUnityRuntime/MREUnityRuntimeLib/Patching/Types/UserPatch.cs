// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Core;
using MixedRealityExtension.Messaging.Payloads.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MixedRealityExtension.Patching.Types
{
	public class UserPatch : Patchable<UserPatch>
	{
		public Guid Id { get; set; }

		[PatchProperty]
		public string Name { get; set; }

		[PatchProperty]
		[JsonConverter(typeof(UnsignedConverter))]
		public UInt32? Groups { get; set; }

		[PatchProperty]
		public Permissions[] GrantedPermissions { get; set; }

		public Dictionary<string, string> Properties { get; set; }

		public UserPatch()
		{
		}

		internal UserPatch(Guid id)
		{
			Id = id;
		}

		internal UserPatch(User user)
			: this(user.Id)
		{
			Name = user.Name;
			Groups = user.Groups;
			// the server doesn't need to care about the execution permission, it's assumed if you're connected
			GrantedPermissions = user.App.GrantedPermissions.ToEnumerable().Where(p => p != Permissions.Execution).ToArray();
			Properties = user.HostAppUser.Properties;
		}
	}
}
