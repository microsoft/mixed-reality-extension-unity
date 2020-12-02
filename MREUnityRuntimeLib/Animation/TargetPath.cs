// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using TokenPoolType = MixedRealityExtension.Animation.JTokenPool.TokenPoolType;
using ITokenLookup = System.Collections.Generic.IReadOnlyDictionary<string, MixedRealityExtension.Animation.JTokenPool.TokenPoolType>;
using TokenLookup = System.Collections.Generic.Dictionary<string, MixedRealityExtension.Animation.JTokenPool.TokenPoolType>;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MixedRealityExtension.Animation
{
	public class TargetPath
	{
		internal static readonly ITokenLookup TypeOfPath = new TokenLookup()
		{
			{"transform/local/position", TokenPoolType.Vector3 },
			{"transform/local/position/x", TokenPoolType.Value },
			{"transform/local/position/y", TokenPoolType.Value },
			{"transform/local/position/z", TokenPoolType.Value },
			{"transform/local/rotation", TokenPoolType.Quaternion },
			{"transform/local/scale", TokenPoolType.Vector3 },
			{"transform/local/scale/x", TokenPoolType.Value },
			{"transform/local/scale/y", TokenPoolType.Value },
			{"transform/local/scale/z", TokenPoolType.Value },
			{"transform/app/position", TokenPoolType.Vector3 },
			{"transform/app/position/x", TokenPoolType.Value },
			{"transform/app/position/y", TokenPoolType.Value },
			{"transform/app/position/z", TokenPoolType.Value },
			{"transform/app/rotation", TokenPoolType.Quaternion },
		};

		private static Regex PathRegex = new Regex("^(?<type>actor|animation|material):(?<placeholder>[^/]+)/(?<path>.+)$");

		public string TargetPathString { get; }

		public string AnimatibleType { get; }

		public string Placeholder { get; }

		public string Path { get; }

		public string[] PathParts { get; }

		public TargetPath(string pathString)
		{
			TargetPathString = pathString;
			var match = PathRegex.Match(TargetPathString);
			try
			{
				AnimatibleType = match.Groups["type"].ToString();
				Placeholder = match.Groups["placeholder"].ToString();
				Path = match.Groups["path"].ToString();
				PathParts = Path.Split('/');
			}
			catch (System.Exception e)
			{
				throw new System.ArgumentException($"{pathString} is not a valid path string.", e);
			}
		}

		public TargetPath ResolvePlaceholder(Guid id)
		{
			return new TargetPath($"{AnimatibleType}:{id}/{Path}");
		}

		public override int GetHashCode()
		{
			return TargetPathString.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return obj != null && GetType() == obj.GetType() && TargetPathString == ((TargetPath)obj).TargetPathString;
		}

		public override string ToString()
		{
			return TargetPathString;
		}
	}
}
