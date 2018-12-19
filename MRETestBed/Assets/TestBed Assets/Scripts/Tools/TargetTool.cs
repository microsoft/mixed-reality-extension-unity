// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Tools
{
    public class TargetTool : Tool
    {
        public GameObject Target { get; private set; }

        protected override void UpdateTool(InputSource inputSource)
        {
            var newTarget = FindTarget(inputSource);
            if (Target == newTarget)
            {
                return;
            }

            if (Target != null)
            {
                var oldBehaviors = Target.GetBehaviors<TargetBehavior>();
                foreach (var behavior in oldBehaviors)
                {
                    var mwUser = behavior.GetMWUnityUser(inputSource.UserGameObject);
                    if (mwUser != null)
                    {
                        behavior.Target.StopAction(mwUser);
                    }
                }
            }

            IEnumerable<TargetBehavior> newBehaviors = null;
            if (newTarget != null)
            {
                newBehaviors = newTarget.GetBehaviors<TargetBehavior>();
                foreach (var behavior in newBehaviors)
                {
                    var mwUser = behavior.GetMWUnityUser(inputSource.UserGameObject);
                    if (mwUser != null)
                    {
                        behavior.Target.StartAction(mwUser);
                    }
                }
            }

            OnTargetChanged(Target, newTarget, inputSource);
            Target = newTarget;

            if (newBehaviors != null)
            {
                var newBehavior = newBehaviors.FirstOrDefault();
                if (newBehavior.GetDesiredToolType() != inputSource.CurrentTool.GetType())
                {
                    inputSource.HoldTool(newBehavior.GetDesiredToolType());
                }
            }
        }

        protected virtual void OnTargetChanged(GameObject oldTarget, GameObject newTarget, InputSource inputSource)
        {

        }

        private GameObject FindTarget(InputSource inputSource)
        {
            RaycastHit hitInfo;
            var gameObject = inputSource.gameObject;
            if (Physics.Raycast(gameObject.transform.position, gameObject.transform.forward, out hitInfo, Mathf.Infinity))
            {
                for (var transform = hitInfo.transform; transform; transform = transform.parent)
                {
                    if (transform.GetComponents<TargetBehavior>().FirstOrDefault() != null)
                    {
                        return transform.gameObject;
                    }
                }
            }

            return null;
        }
    }
}
