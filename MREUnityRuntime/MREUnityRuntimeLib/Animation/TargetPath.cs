// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MixedRealityExtension.Animation
{
	public class TargetPath
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
				try
				{

					AnimatibleType = match.Groups["type"].ToString();
					Placeholder = match.Groups["placeholder"].ToString();
					Path = match.Groups["path"].ToString();
					PathParts = Path.Split('/');
				}
				catch (System.Exception e)
				{
					Debug.LogFormat("Error parsing target path {0}", PathString);
					Debug.LogException(e);
				}
			}
		}
	}
}
