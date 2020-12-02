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
	/// Animation Event
	/// </summary>
	public class MWAnimationEvent
	{
		/// <summary>
		/// The animation event name
		/// </summary>
		public string Name;

		/// <summary>
		/// The animation event value
		/// </summary>
		public string Value;

		/// <summary>
		/// The time offset (in seconds) when the animation event should be raised
		/// </summary>
		public float Time;
	}
}
