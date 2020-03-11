// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Animation
{
	internal static class Interpolations
	{
		internal static void Interpolate(JToken a, JToken b, float t, ref JToken mix, (float, float, float, float)? easing)
		{
			var ratio = GetEasing(t, easing);

			if (a.Type == JTokenType.Object)
			{
				JObject A = (JObject)a;
				JObject B = (JObject)b;
				JObject Mix = (JObject)mix;

				// quaternion
				if (A.ContainsKey("x") && A.ContainsKey("y") && A.ContainsKey("z") && A.ContainsKey("w"))
				{
					UnityEngine.Quaternion.Slerp()
				}
			}
		}

		private static float GetEasing(float t, (float, float, float, float)? easing)
		{
			if (easing == null)
			{
				easing = (0f, 0f, 1f, 1f);
			}
			float x1 = easing.Value.Item1, y1 = easing.Value.Item2, x2 = easing.Value.Item3, y2 = easing.Value.Item4;

			// cubic bezier solver borrowed from Babylon.js's Bezier curve implementation
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
