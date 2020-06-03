// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using UnityEngine;

namespace Assets.Scripts.Tools
{
	public class ButtonTool : TargetTool
	{
		protected override void UpdateTool(InputSource inputSource)
		{
			base.UpdateTool(inputSource);

			if (Target == null)
			{
				return;
			}

			if (Input.GetButtonDown("Fire1"))
			{
				var buttonBehavior = Target.GetBehavior<ButtonBehavior>();
				if (buttonBehavior != null)
				{
					var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserGameObject);
					if (mwUser != null)
					{
						buttonBehavior.Context.StartButton(mwUser, CurrentTargetPoint);
					}
				}
			}
			else if (Input.GetButtonUp("Fire1"))
			{
				var buttonBehavior = Target.GetBehavior<ButtonBehavior>();
				if (buttonBehavior != null)
				{
					var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserGameObject);
					if (mwUser != null)
					{
						buttonBehavior.Context.EndButton(mwUser, CurrentTargetPoint);
						buttonBehavior.Context.Click(mwUser, CurrentTargetPoint);
					}
				}
			}
		}

		protected override void OnTargetChanged(
			GameObject oldTarget,
			Vector3 oldTargetPosition,
			GameObject newTarget,
			Vector3 newTargetPosition,
			TargetBehavior newBehavior,
			InputSource inputSource)
		{
			base.OnTargetChanged(
				oldTarget,
				oldTargetPosition,
				newTarget,
				newTargetPosition,
				newBehavior,
				inputSource);

			if (oldTarget != null)
			{
				var oldBehavior = oldTarget.GetBehavior<ButtonBehavior>();
				if (oldBehavior != null)
				{
					var mwUser = oldBehavior.GetMWUnityUser(inputSource.UserGameObject);
					if (mwUser != null)
					{
						oldBehavior.Context.EndHover(mwUser, oldTargetPosition);
					}
				}
			}

			if (newTarget != null)
			{
				var newButtonBehavior = newBehavior as ButtonBehavior;
				if (newButtonBehavior != null)
				{
					var mwUser = newButtonBehavior.GetMWUnityUser(inputSource.UserGameObject);
					if (mwUser != null)
					{
						newButtonBehavior.Context.StartHover(mwUser, newTargetPosition);
					}
				}
			}
		}
	}
}
