// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using CubicBezier = MixedRealityExtension.Util.CubicBezier;
using Newtonsoft.Json.Linq;
using System;
using Quaternion = UnityEngine.Quaternion;
using UnityMath = UnityEngine.Mathf;

namespace MixedRealityExtension.Animation
{
	internal static class Interpolations
	{
		internal static void Interpolate(JToken a, JToken b, float linearT, ref JToken mix, CubicBezier easing)
		{
			var easedT = easing?.Sample(linearT) ?? 0;

			// compound types
			if (a.Type == JTokenType.Object)
			{
				JObject A = (JObject)a;
				JObject B = (JObject)b;
				JObject Mix = (JObject)mix;
				Mix.RemoveAll();

				if (A.ContainsKey("x") && A.ContainsKey("y"))
				{
					if (A.ContainsKey("z"))
					{
						// quaternion
						if (A.ContainsKey("w"))
						{
							Quaternion q1 = new Quaternion(A.Value<float>("x"), A.Value<float>("y"), A.Value<float>("z"), A.Value<float>("w"));
							Quaternion q2 = new Quaternion(B.Value<float>("x"), B.Value<float>("y"), B.Value<float>("z"), B.Value<float>("w"));
							var qMix = Quaternion.Slerp(q1, q2, easedT);
							Mix.Add("x", qMix.x);
							Mix.Add("y", qMix.y);
							Mix.Add("z", qMix.z);
							Mix.Add("w", qMix.w);
						}
						// Vector3
						else
						{
							Mix.Add("x", UnityMath.Lerp(A.Value<float>("x"), B.Value<float>("x"), easedT));
							Mix.Add("y", UnityMath.Lerp(A.Value<float>("y"), B.Value<float>("y"), easedT));
							Mix.Add("z", UnityMath.Lerp(A.Value<float>("z"), B.Value<float>("z"), easedT));
						}
					}
					// Vector2
					else
					{
						Mix.Add("x", UnityMath.Lerp(A.Value<float>("x"), B.Value<float>("x"), easedT));
						Mix.Add("y", UnityMath.Lerp(A.Value<float>("y"), B.Value<float>("y"), easedT));
					}
				}
				// TODO: other compound types (color3, color4)
			}
			// simple types
			else
			{
				JValue A = (JValue)a;
				JValue B = (JValue)b;
				JValue Mix = (JValue)mix;

				// numeric types
				if (a.Type == JTokenType.Float || a.Type == JTokenType.Integer)
				{
					Mix.Value = UnityMath.Lerp(A.Value<float>(), B.Value<float>(), easedT);
				}
				// no easing available, just use A
				else
				{
					Mix.Value = A.Value;
				}
			}
		}
	}
}
