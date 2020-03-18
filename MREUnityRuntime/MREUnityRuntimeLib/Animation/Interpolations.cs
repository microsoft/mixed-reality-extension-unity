// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Newtonsoft.Json.Linq;
using System;

namespace MixedRealityExtension.Animation
{
	internal static class Interpolations
	{
		internal static void Interpolate(JToken a, JToken b, float linearT, ref JToken mix, float[] easing)
		{
			var easedT = GetEasing(linearT, easing);

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
							UnityEngine.Quaternion q1 = new UnityEngine.Quaternion(A.Value<float>("x"), A.Value<float>("y"), A.Value<float>("z"), A.Value<float>("w"));
							UnityEngine.Quaternion q2 = new UnityEngine.Quaternion(B.Value<float>("x"), B.Value<float>("y"), B.Value<float>("z"), B.Value<float>("w"));
							var qMix = UnityEngine.Quaternion.Slerp(q1, q2, easedT);
							Mix.Add("x", qMix.x);
							Mix.Add("y", qMix.y);
							Mix.Add("z", qMix.z);
							Mix.Add("w", qMix.w);
						}
						// Vector3
						else
						{
							Mix.Add("x", (1 - easedT) * A.Value<float>("x") + easedT * B.Value<float>("x"));
							Mix.Add("y", (1 - easedT) * A.Value<float>("y") + easedT * B.Value<float>("y"));
							Mix.Add("z", (1 - easedT) * A.Value<float>("z") + easedT * B.Value<float>("z"));
						}
					}
					// Vector2
					else
					{
						Mix.Add("x", (1 - easedT) * A.Value<float>("x") + easedT * B.Value<float>("x"));
						Mix.Add("y", (1 - easedT) * A.Value<float>("y") + easedT * B.Value<float>("y"));
					}
				}
			}
		}

		private static float GetEasing(float t, float[] easing)
		{
			// special case step
			if (easing == null || easing.Length != 4)
			{
				return 0;
			}
			// special case linear: skip all the math
			else if (easing[0] == 0 && easing[1] == 0 && easing[2] == 1 && easing[3] == 1)
			{
				return t;
			}

			// cubic bezier solver borrowed from Babylon.js's Bezier curve implementation
			float x1 = easing[0], y1 = easing[1], x2 = easing[2], y2 = easing[3];
			double f0 = 1 - 3 * x2 + 3 * x1;
			double f1 = 3 * x2 - 6 * x1;
			double f2 = 3 * x1;

			double refinedT = t;
			for (var i = 0; i < 5; i++)
			{
				double refinedT2 = refinedT * refinedT;
				double refinedT3 = refinedT2 * refinedT;

				double x = f0 * refinedT3 + f1 * refinedT2 + f2 * refinedT;
				double slope = 1 / (3 * f0 * refinedT2 + 2 * f1 * refinedT + f2);
				refinedT -= (x - t) * slope;
				refinedT = Math.Min(1, Math.Max(0, refinedT));
			}

			return (float)(
				3 * Math.Pow(1 - refinedT, 2) * refinedT * y1 +
				3 * (1 - refinedT) * Math.Pow(refinedT, 2) * y2 +
				Math.Pow(refinedT, 3)
			);
		}
	}
}
