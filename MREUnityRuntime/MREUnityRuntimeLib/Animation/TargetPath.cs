// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using JTokenType = Newtonsoft.Json.Linq.JTokenType;
using ITokenLookup = System.Collections.Generic.IReadOnlyDictionary<string, Newtonsoft.Json.Linq.JTokenType>;
using TokenLookup = System.Collections.Generic.Dictionary<string, Newtonsoft.Json.Linq.JTokenType>;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MixedRealityExtension.Animation
{
	public class TargetPath
	{
		public static readonly ITokenLookup TypeOfPath = new TokenLookup()
		{
			{"transform/local/position", JTokenType.Object },
			{"transform/local/position/x", JTokenType.Float },
			{"transform/local/position/y", JTokenType.Float },
			{"transform/local/position/z", JTokenType.Float },
			{"transform/local/rotation", JTokenType.Object },
			{"transform/local/scale", JTokenType.Object },
			{"transform/local/scale/x", JTokenType.Float },
			{"transform/local/scale/y", JTokenType.Float },
			{"transform/local/scale/z", JTokenType.Float },
			{"transform/app/position", JTokenType.Object },
			{"transform/app/position/x", JTokenType.Float },
			{"transform/app/position/y", JTokenType.Float },
			{"transform/app/position/z", JTokenType.Float },
			{"transform/app/rotation", JTokenType.Object },
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
	}
}
