// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using MixedRealityExtension.App;

namespace MixedRealityExtension.PluginInterfaces
{
	public interface IDialogFactory
	{
		void ShowDialog(IMixedRealityExtensionApp app, string text, bool acceptInput, Action<bool, string> callback);
	}
}
