using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Util.Unity;
using System.Collections.Generic;
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
			var targetData = new TargetData()
			{
				targetPoints = new MWVector3[1]
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
				targetPoints = new MWVector3[1]
				{
					targetPoint.CreateMWVector3()
				}
			};

			_targetAction.StopAction(user, targetData);
		}

		public void UpdateTargetPoint(IUser user, Vector3 targetPoint)
		{
			_currentTargetPoints.Add(targetPoint);
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
			RegisterActionHandler(_targetAction, nameof(_targetAction));
			RegisterActionHandler(_grabAction, nameof(_grabAction));
		}

		// TODO @tombu - Add in the synchronize call here for sending up the target point queue and flushing.
	}
}
