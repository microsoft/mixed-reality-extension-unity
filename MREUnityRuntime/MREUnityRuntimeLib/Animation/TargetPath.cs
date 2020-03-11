// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MixedRealityExtension.Animation
{
	internal class TargetPath
	{
		private static Regex PathRegex = new Regex("^(?<type>actor|animation|material):(?<placeholder>[^/]+)/(?<path>.+)$");

		public string PathString { get; }

		public string AnimatibleType { get; }

		public string Placeholder { get; }

		public string Path { get; }

		public string[] PathParts { get; }

		public TargetPath(string pathString)
		{
			PathString = pathString;
			var match = PathRegex.Match(PathString);
			if (match.Success)
			{
				AnimatibleType = match.Captures[0].ToString();
				Placeholder = match.Captures[1].ToString();
				Path = match.Captures[2].ToString();
				PathParts = Path.Split('/');
			}
		}
	}
}
