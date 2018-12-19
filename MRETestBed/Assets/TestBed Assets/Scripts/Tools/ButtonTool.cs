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
                foreach (var buttonBehavior in Target.GetBehaviors<ButtonBehavior>())
                {
                    var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserGameObject);
                    if (mwUser != null)
                    {
                        buttonBehavior.Click.StartAction(mwUser);
                    }
                }
            }
            else if (Input.GetButtonUp("Fire1"))
            {
                foreach (var buttonBehavior in Target.GetBehaviors<ButtonBehavior>())
                {
                    var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserGameObject);
                    if (mwUser != null)
                    {
                        buttonBehavior.Click.StopAction(mwUser);
                    }
                }
            }
        }

        protected override void OnTargetChanged(GameObject oldTarget, GameObject newTarget, InputSource inputSource)
        {
            base.OnTargetChanged(oldTarget, newTarget, inputSource);

            if (oldTarget != null)
            {
                var oldBehaviors = oldTarget.GetBehaviors<ButtonBehavior>();
                foreach (var buttonBehavior in oldBehaviors)
                {
                    var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserGameObject);
                    if (mwUser != null)
                    {
                        buttonBehavior.Hover.StopAction(mwUser);
                    }
                }
            }

            
            if (newTarget != null)
            {
                var newBehaviors = newTarget.GetBehaviors<ButtonBehavior>();
                foreach (var buttonBehavior in newBehaviors)
                {
                    var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserGameObject);
                    if (mwUser != null)
                    {
                        buttonBehavior.Hover.StartAction(mwUser);
                    }
                }
            }
        }
    }
}
