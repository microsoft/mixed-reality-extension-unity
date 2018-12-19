// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using UnityEngine;

namespace Assets.Scripts.Tools
{
    public class GrabTool : TargetTool
    {
        protected override void UpdateTool(InputSource inputSource)
        {
            base.UpdateTool(inputSource);

            if (Target == null)
            {
                return;
            }

            if (Input.GetButtonDown("Fire2"))
            {
                foreach (var grabBehavior in Target.GetBehaviors<TargetBehavior>())
                {
                    var mwUser = grabBehavior.GetMWUnityUser(inputSource.UserGameObject);
                    if (mwUser != null)
                    {
                        //grabBehavior.Grab.StartAction(mwUser);
                    }
                }
            }
            else if (Input.GetButtonUp("Fire2"))
            {
                foreach (var grabBehavior in Target.GetBehaviors<TargetBehavior>())
                {
                    var mwUser = grabBehavior.GetMWUnityUser(inputSource.UserGameObject);
                    if (mwUser != null)
                    {
                        //grabBehavior.Grab.StopAction(mwUser);
                    }
                }
            }
        }
    }
}
