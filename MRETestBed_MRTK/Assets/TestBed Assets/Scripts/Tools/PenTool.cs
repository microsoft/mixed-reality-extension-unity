// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using UnityEngine;

namespace Assets.Scripts.Tools
{
	public class PenTool : TargetTool
	{
		protected override void UpdateTool(InputSource inputSource)
		{
			base.UpdateTool(inputSource);

			if (Target == null || !TargetGrabbed)
			{
				return;
			}

			if (Input.GetButtonDown("Fire1"))
			{
				var penBehavior = Target.GetBehavior<PenBehavior>();
				if (penBehavior != null)
				{
					var mwUser = penBehavior.GetMWUnityUser(inputSource.UserGameObject);
					if (mwUser != null)
					{
						penBehavior.Context.StartUsing(mwUser);
					}
				}
			}
			else if (Input.GetButtonUp("Fire1"))
			{
				var penBehavior = Target.GetBehavior<PenBehavior>();
				if (penBehavior != null)
				{
					var mwUser = penBehavior.GetMWUnityUser(inputSource.UserGameObject);
					if (mwUser != null)
					{
						penBehavior.Context.EndUsing(mwUser);
					}
				}
			}
		}

		protected override void OnGrabStateChanged(GrabState oldGrabState, GrabState newGrabState, InputSource inputSource)
		{
			base.OnGrabStateChanged(oldGrabState, newGrabState, inputSource);

			var penBehavior = Target.GetBehavior<PenBehavior>();
			if (penBehavior != null)
			{
				var mwUser = penBehavior.GetMWUnityUser(inputSource.UserGameObject);
				if (mwUser != null)
				{
					if (newGrabState == GrabState.Grabbed)
					{
						penBehavior.Context.StartHolding(mwUser);
					}
					else
					{
						penBehavior.Context.EndHolding(mwUser);
					}
				}
			}
		}
	}
}
