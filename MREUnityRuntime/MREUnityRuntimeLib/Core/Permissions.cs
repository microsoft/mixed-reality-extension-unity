// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Core
{
	[Flags]
	public enum Permissions
	{
		None = 0,
		Execution = 1,
		UserTracking = 2,
		UserInteraction = 4
	}
}
