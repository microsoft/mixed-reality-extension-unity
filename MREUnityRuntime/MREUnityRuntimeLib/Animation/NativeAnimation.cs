// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using MixedRealityExtension.Core;
using MixedRealityExtension.Patching.Types;
using UnityEngine;

namespace MixedRealityExtension.Animation
{
	internal class NativeAnimation : Animation
	{
		private AnimationState nativeAnimation;

		public override uint BasisTime { get; protected set; }

		public override float Time { get; protected set; }

		public override float Speed
		{
			get => nativeAnimation.speed;
			protected set { nativeAnimation.speed = value; }
		}

		public override float Weight
		{
			get => nativeAnimation.weight;
			protected set { nativeAnimation.weight = value; }
		}

		public override MWAnimationWrapMode WrapMode
		{
			get
			{
				switch(nativeAnimation.wrapMode)
				{
					case UnityEngine.WrapMode.Loop:
						return MWAnimationWrapMode.Loop;
					case UnityEngine.WrapMode.PingPong:
						return MWAnimationWrapMode.PingPong;
					default:
						return MWAnimationWrapMode.Once;
				}
			}
			protected set
			{

			}
		}

		internal NativeAnimation(Guid id, AnimationState nativeAnimation)
		{
			Id = id;
			this.nativeAnimation = nativeAnimation;
		}

		public override AnimationPatch GeneratePatch()
		{
			var patch = base.GeneratePatch();
			patch.Duration = nativeAnimation.length;
			return patch;
		}
	}
}
