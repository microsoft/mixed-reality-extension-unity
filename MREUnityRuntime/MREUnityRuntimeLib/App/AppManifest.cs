// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Core
{
	public class AppManifest
	{
		public string Name;

		public string Description;

		public string Author;

		public string License;

		public string RepositoryUri;

		public Permissions[] Permissions;

		public Permissions[] OptionalPermissions;

		internal static async Task<AppManifest> DownloadManifest(Uri manifestUri)
		{
			var webClient = new HttpClient();
			var response = await webClient.GetAsync(manifestUri, HttpCompletionOption.ResponseContentRead);
			if (response.IsSuccessStatusCode)
			{

				var manifestString = await response.Content.ReadAsStringAsync();
			}
		}
	}
}
