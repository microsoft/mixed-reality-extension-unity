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
		private MWAction<TargetData> _targetAction = new MWAction<TargetData>();
		private MWAction _grabAction = new MWAction();

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

			_targetAction.StartAction(user, targetData);
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

			_targetAction.StopAction(user, targetData);
		}

		public void UpdateTargetPoint(IUser user, Vector3 targetPoint)
		{
			_currentTargetPoints.Add(targetPoint);
			OnTargetPointUpdated(targetPoint);
		}

		public void StartGrab(IUser user)
		{
			_grabAction.StartAction(user);
		}

		public void EndGrab(IUser user)
		{
			_grabAction.StopAction(user);
		}

		internal TargetBehaviorContext()
			: base()
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
				_targetAction.PerformActionUpdate(new TargetData()
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
			RegisterActionHandler(_targetAction, "target");
			RegisterActionHandler(_grabAction, "grab");
		}
	}
}
