// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Tools
{
	public static class ToolCache
	{
		public static Dictionary<Type, Tool> _cachedAvailableTools = new Dictionary<Type, Tool>();

		public static Tool GetOrCreateTool<ToolT>() where ToolT : Tool, new()
		{
			return GetOrCreateTool(typeof(ToolT));
		}

		public static Tool GetOrCreateTool(Type toolType)
		{
			
			if (_cachedAvailableTools.ContainsKey(toolType))
			{
				var cachedTool = _cachedAvailableTools[toolType];
				_cachedAvailableTools.Remove(toolType);
				return cachedTool;
			}

			return (Tool)Activator.CreateInstance(toolType);
		}

		public static void StowTool(Tool tool)
		{
			_cachedAvailableTools[typeof(Tool)] = tool;
		}
	}
}
