// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Tools
{
	public static class ToolUtils
	{
		public static BehaviorT GetBehavior<BehaviorT>(this GameObject _this) 
			where BehaviorT : IBehavior
		{
			return _this.GetComponents<BehaviorT>().FirstOrDefault();
		}
	}
}
