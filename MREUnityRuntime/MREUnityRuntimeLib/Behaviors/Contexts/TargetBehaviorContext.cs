using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Util.Unity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MixedRealityExtension.Behaviors.Contexts
{
	public class TargetBehaviorContext : BehaviorContextBase
	{
		private List<MWVector3> _currentTargetPoints = new List<MWVector3>();
		private MWAction<TargetData> _targetAction = new MWAction<TargetData>();
		private MWAction _grabAction = new MWAction();

		public void StartTargeting(IUser user, Vector3 targetPoint)
		{
			var targetData = new TargetData()
			{
				targetedPoints = new MWVector3[1]
				{
					targetPoint.CreateMWVector3()
				}
			};

			_targetAction.StartAction(user, targetData);
		}

		public void EndTargeting(IUser user, Vector3 targetPoint)
		{
			var targetData = new TargetData()
			{
				targetedPoints = new MWVector3[1]
				{
					targetPoint.CreateMWVector3()
				}
			};

			_targetAction.StopAction(user, targetData);
		}

		public void UpdateTargetPoint(IUser user, Vector3 targetPoint)
		{
			_currentTargetPoints.Add(targetPoint.CreateMWVector3());
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

			if (_currentTargetPoints.Any())
			{
				_targetAction.PerformActionUpdate(new TargetData()
				{
					targetedPoints = _currentTargetPoints.ToArray()
				});

				_currentTargetPoints.Clear();
			}
		}

		protected virtual void OnTargetPointUpdated(Vector3 point)
		{

		}

		protected override void OnInitialized()
		{
			RegisterActionHandler(_targetAction, nameof(_targetAction));
			RegisterActionHandler(_grabAction, nameof(_grabAction));
		}
	}
}
