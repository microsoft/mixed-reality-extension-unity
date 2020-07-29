// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Core
{
	/// <summary>
	/// Class containing author-provided metadata about an MRE instance
	/// </summary>
	public class AppManifest
	{
		/// <summary>
		/// A human-readable name for this MRE
		/// </summary>
		public string Name;

		/// <summary>
		/// A human readable description of this MRE's behavior
		/// </summary>
		public string Description;

		/// <summary>
		/// The MRE's author name and/or contact information
		/// </summary>
		public string Author;

		/// <summary>
		/// The license for the MRE's source code
		/// </summary>
		public string License;

		/// <summary>
		/// The location of the MRE's public source code
		/// </summary>
		public string RepositoryUrl;

		/// <summary>
		/// A list of permissions required for this MRE to run
		/// </summary>
		public Permissions[] Permissions;

		/// <summary>
		/// A list of permissions that this MRE can use, but are not required
		/// </summary>
		public Permissions[] OptionalPermissions;

		internal static async Task<AppManifest> DownloadManifest(Uri manifestUri)
		{
			var webClient = new HttpClient();
			var response = await webClient.GetAsync(manifestUri, HttpCompletionOption.ResponseContentRead);
			response.EnsureSuccessStatusCode();

			var manifestString = await response.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<AppManifest>(manifestString, Constants.SerializerSettings);
		}
	}
}
