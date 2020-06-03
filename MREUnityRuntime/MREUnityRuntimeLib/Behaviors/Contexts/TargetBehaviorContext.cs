// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MixedRealityExtension.Behaviors.Contexts
{
	public class TargetBehaviorContext : BehaviorContextBase
	{
		private List<Vector3> _currentTargetPoints = new List<Vector3>();

		internal MWAction<TargetData> TargetAction { get; } = new MWAction<TargetData>();

		internal MWAction GrabAction { get; } = new MWAction();

		public void StartTargeting(IUser user, Vector3 targetPoint)
		{
			var app = App;
			if (app == null)
			{
				return;
			}

			var targetData = new TargetData()
			{
				targetedPoints = new PointData[1]
				{
					PointData.CreateFromUnityVector3(targetPoint, Behavior.Actor.GameObject.transform, app.SceneRoot.transform)
				}
			};

			TargetAction.StartAction(user, targetData);
		}

		public void EndTargeting(IUser user, Vector3 targetPoint)
		{
			var app = App;
			if (app == null)
			{
				return;
			}

			var targetData = new TargetData()
			{
				targetedPoints = new PointData[1]
				{
					PointData.CreateFromUnityVector3(targetPoint, Behavior.Actor.GameObject.transform, app.SceneRoot.transform)
				}
			};

			TargetAction.StopAction(user, targetData);
		}

		public void UpdateTargetPoint(IUser user, Vector3 targetPoint)
		{
			_currentTargetPoints.Add(targetPoint);
			OnTargetPointUpdated(targetPoint);
		}

		public void StartGrab(IUser user)
		{
			GrabAction.StartAction(user);
		}

		public void EndGrab(IUser user)
		{
			GrabAction.StopAction(user);
		}

		internal TargetBehaviorContext()
		{
			
		}
		
		internal override void SynchronizeBehavior()
		{
			base.SynchronizeBehavior();

			var app = App;
			if (app == null)
			{
				return;
			}

			if (_currentTargetPoints.Any())
			{
				TargetAction.PerformActionUpdate(new TargetData()
				{
					targetedPoints = _currentTargetPoints.Select((point) =>
					{
						return PointData.CreateFromUnityVector3(point, Behavior.Actor.GameObject.transform, App.SceneRoot.transform);
					}).ToArray()
				});

				_currentTargetPoints.Clear();
			}
		}

		protected virtual void OnTargetPointUpdated(Vector3 point)
		{

		}

		protected override void OnInitialized()
		{
			RegisterActionHandler(TargetAction, "target");
			RegisterActionHandler(GrabAction, "grab");
		}
	}
}
