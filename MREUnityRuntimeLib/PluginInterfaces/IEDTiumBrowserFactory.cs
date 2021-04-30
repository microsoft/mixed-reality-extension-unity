// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Assets;
using MixedRealityExtension.Util.Unity;

namespace MixedRealityExtension.PluginInterfaces
{
	/*public struct FetchResult
	{
		public VideoStreamDescription Asset;
		public string FailureMessage;
	}*/

	/// <summary>
	/// A factory class that instantiates an EDTium Browser
	/// </summary>
	public interface IEDTiumBrowserFactory
	{
		IEDTiumBrowser CreateBrowser(IActor parent, Action<BrowserStateOptions> stateChanged);
	}
}
