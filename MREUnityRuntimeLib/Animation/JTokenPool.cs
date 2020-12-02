// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MixedRealityExtension.Animation
{
	internal class JTokenPool
	{
		public enum TokenPoolType
		{
			Value = 0,
			Vector3,
			Quaternion
		}

		private readonly Dictionary<TokenPoolType, Stack<JToken>> TokenPool = new Dictionary<TokenPoolType, Stack<JToken>>(3)
		{
			{TokenPoolType.Value, new Stack<JToken>(5) },
			{TokenPoolType.Vector3, new Stack<JToken>(5) },
			{TokenPoolType.Quaternion, new Stack<JToken>(5) }
		};

		/// <summary>
		/// Produce a JToken of the requested shape
		/// </summary>
		/// <param name="matchingType">The produced token will have the same fields as this token</param>
		/// <returns></returns>
		public JToken Lease(JToken matchingType)
		{
			return Lease(DetermineType(matchingType));
		}

		public JToken Lease(TokenPoolType type)
		{
			var pool = TokenPool[type];
			return pool.Count > 0 ? pool.Pop() : GenerateType(type);
		}

		public void Return(JToken token)
		{
			TokenPool[DetermineType(token)].Push(token);
		}

		private TokenPoolType DetermineType(JToken token)
		{
			if (token.Type == JTokenType.Object)
			{
				var Token = (JObject)token;
				if (Token.ContainsKey("w"))
				{
					return TokenPoolType.Quaternion;
				}
				else
				{
					return TokenPoolType.Vector3;
				}
			}
			else
			{
				return TokenPoolType.Value;
			}
		}

		private JToken GenerateType(TokenPoolType type)
		{
			switch (type)
			{
				case TokenPoolType.Quaternion:
					return new JObject()
					{
						{"x", new JValue(0) },
						{"y", new JValue(0) },
						{"z", new JValue(0) },
						{"w", new JValue(0) }
					};
				case TokenPoolType.Vector3:
					return new JObject()
					{
						{"x", new JValue(0) },
						{"y", new JValue(0) },
						{"z", new JValue(0) }
					};
				case TokenPoolType.Value:
					return new JValue(0);
				default:
					throw new System.NotSupportedException("Cannot allocate token of unknown type");
			}
		}
	}
}
