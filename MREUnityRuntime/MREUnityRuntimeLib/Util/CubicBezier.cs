// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Util
{
	/// <summary>
	/// Calculates a value along a two-point bezier curve.
	/// Implementation based on this article:
	/// http://greweb.me/2012/02/bezier-curve-based-easing-functions-from-concept-to-implementation/
	/// and https://github.com/gre/bezier-easing
	/// </summary>
	public class CubicBezier
	{
		private readonly float mX1;
		private readonly float mY1;
		private readonly float mX2;
		private readonly float mY2;

		private const int SPLINE_TABLE_SIZE = 11;
		private const float SAMPLE_STEP_SIZE = 1.0f / (SPLINE_TABLE_SIZE - 1);

		private readonly float[] mSampleValues = new float[SPLINE_TABLE_SIZE];

		/// <summary>
		/// Creates a CubicBezier instance.
		/// </summary>
		/// <param name="x1"></param>
		/// <param name="y1"></param>
		/// <param name="x2"></param>
		/// <param name="y2"></param>
		public CubicBezier(float x1, float y1, float x2, float y2)
		{
			mX1 = x1;
			mY1 = y1;
			mX2 = x2;
			mY2 = y2;

			// Precalculate some sample points to make sample calculation more performant.
			if (mX1 != mY1 || mX2 != mY2)
			{
				for (int i = 0; i < SPLINE_TABLE_SIZE; ++i)
				{
					mSampleValues[i] = CalcBezier(i * SAMPLE_STEP_SIZE, mX1, mX2);
				}
			}
		}

		/// <summary>
		/// Calculate the value at the given point along the curve.
		/// </summary>
		/// <param name="val">The location at which to sample the curve. Must be in [0, 1] range.</param>
		/// <returns>The calculated value.</returns>
		public float Sample(float val)
		{
			// special case all zeros: no interpolation
			if (val <= 0 || (mX1 == 0 && mY1 == 0 && mX2 == 0 && mY2 == 0))
			{
				return 0;
			}
			if (val >= 1)
			{
				return 1;
			}
			if (mX1 == mY1 && mX2 == mY2)
			{
				// Early-out for linear
				return val;
			}
			return CalcBezier(GetTForX(val), mY1, mY2);
		}

		private static float A(float aA1, float aA2) => 1.0f - 3.0f * aA2 + 3.0f * aA1;
		private static float B(float aA1, float aA2) => 3.0f * aA2 - 6.0f * aA1;
		private static float C(float aA1) => 3.0f * aA1;
		// Use Horner's method to calculate the polynomial.
		// https://en.wikipedia.org/wiki/Horner%27s_method
		private static float CalcBezier(float t, float aA1, float aA2) => ((A(aA1, aA2) * t + B(aA1, aA2)) * t + C(aA1)) * t;
		private static float GetSlope(float t, float aA1, float aA2) => 3.0f * A(aA1, aA2) * t * t + 2.0f * B(aA1, aA2) * t + C(aA1);

		private const float NEWTON_MIN_SLOPE = 0.001f;

		private float GetTForX(float aX)
		{
			// Find the interval where t lies to get us in the ballpark.
			float intervalStart = 0.0f;
			int currentSample = 1;
			int lastSample = SPLINE_TABLE_SIZE - 1;
			for (; currentSample != lastSample && mSampleValues[currentSample] <= aX; ++currentSample)
			{
				intervalStart += SAMPLE_STEP_SIZE;
			}
			--currentSample;

			// Calulate the initial guess for t.
			float dist = (aX - mSampleValues[currentSample]) / (mSampleValues[currentSample + 1] - mSampleValues[currentSample]);
			float guessForT = intervalStart + dist * SAMPLE_STEP_SIZE;

			// If the slope is too small, Newton-Raphson iteration won't converge on a root, so use dichotomic search.
			float initialSlope = GetSlope(guessForT, mX1, mX2);
			if (initialSlope >= NEWTON_MIN_SLOPE)
			{
				return NewtonRaphsonIterate(aX, guessForT);
			}
			else if (initialSlope == 0.0)
			{
				return guessForT;
			}
			else
			{
				return BinarySubdivide(aX, intervalStart, intervalStart + SAMPLE_STEP_SIZE);
			}
		}

		private const int NEWTON_ITERATIONS = 4;

		private float NewtonRaphsonIterate(float aX, float aGuessT)
		{
			for (int i = 0; i < NEWTON_ITERATIONS; ++i)
			{
				float currentSlope = GetSlope(aGuessT, mX1, mX2);
				if (currentSlope == 0.0f)
				{
					return aGuessT;
				}
				float currentX = CalcBezier(aGuessT, mX1, mX2) - aX;
				aGuessT -= currentX / currentSlope;
			}
			return aGuessT;
		}

		private const float SUBDIVISION_PRECISION = 0.0000001f;
		private const int SUBDIVISION_MAX_ITERATIONS = 10;

		private float BinarySubdivide(float aX, float aA, float aB)
		{
			float currentX;
			float currentT;
			int i = 0;

			do
			{
				currentT = aA + (aB - aA) / 2.0f;
				currentX = CalcBezier(currentT, mX1, mX2) - aX;

				if (currentX > 0.0)
				{
					aB = currentT;
				}
				else
				{
					aA = currentT;
				}
			} while (Math.Abs(currentX) > SUBDIVISION_PRECISION && ++i < SUBDIVISION_MAX_ITERATIONS);

			return currentT;
		}
	}
}
