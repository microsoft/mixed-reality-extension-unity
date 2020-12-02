// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Animation
{
	/// <summary>
	/// Animation Wrap Mode
	/// </summary>
	public enum MWAnimationWrapMode
	{
		/// <summary>
		/// At the end of the animation, stop playing and reset time to zero.
		/// </summary>
		Once,

		/// <summary>
		/// At the end of the animation, restart at the beginning.
		/// </summary>
		Loop,

		/// <summary>
		/// At the end of the animation, run the animation backward to the beginning, and vice versa.
		/// </summary>
		PingPong,
	}

	/// <summary>
	/// Extension methods for MWAnimationWrapMode enumeration.
	/// </summary>
	public static class MWAnimationWrapModeExtensions
	{
		/// <summary>
		/// Returns true if the mode is a looping mode.
		/// </summary>
		/// <param name="wrapMode"></param>
		/// <returns></returns>
		public static bool IsLooping(this MWAnimationWrapMode wrapMode)
		{
			return (wrapMode == MWAnimationWrapMode.Loop || wrapMode == MWAnimationWrapMode.PingPong);
		}
	}
}
