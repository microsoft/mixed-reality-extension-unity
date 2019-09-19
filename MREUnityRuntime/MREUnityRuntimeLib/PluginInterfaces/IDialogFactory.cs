// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace MixedRealityExtension.PluginInterfaces
{
	public interface IDialogFactory
	{
		void ShowDialog(string text, bool allowInput, Action<bool, string> callback);
	}
}
