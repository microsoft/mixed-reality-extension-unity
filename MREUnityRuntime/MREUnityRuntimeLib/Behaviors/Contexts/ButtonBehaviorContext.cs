using MixedRealityExtension.App;
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
	public class ButtonBehaviorContext : TargetBehaviorContext
	{
		private MWAction<ButtonData> _hoverAction = new MWAction<ButtonData>();
		private MWAction<ButtonData> _clickAction = new MWAction<ButtonData>();
		private MWAction<ButtonData> _buttonAction = new MWAction<ButtonData>();
		private List<MWVector3> _buttonPressedPoints = new List<MWVector3>();
		private List<MWVector3> _hoverPoints = new List<MWVector3>();

		public bool IsPressed { get; private set; } 

		public void StartHover(IUser user, Vector3 hoverPoint)
		{
			_hoverAction.StartAction(user, new ButtonData()
			{
				targetedPoints = new MWVector3[1] { hoverPoint.CreateMWVector3() }
			});
		}

		public void EndHover(IUser user, Vector3 hoverPoint)
		{
			_hoverAction.StopAction(user, new ButtonData()
			{
				targetedPoints = new MWVector3[1] { hoverPoint.CreateMWVector3() }
			});
		}

		public void StartButton(IUser user, Vector3 buttonStartPoint)
		{
			_buttonAction.StartAction(user, new ButtonData()
			{
				targetedPoints = new MWVector3[1] { buttonStartPoint.CreateMWVector3() }
			});

			IsPressed = true;
		}

		public void EndButton(IUser user, Vector3 buttonEndPoint)
		{
			_buttonAction.StartAction(user, new ButtonData()
			{
				targetedPoints = new MWVector3[1] { buttonEndPoint.CreateMWVector3() }
			});

			IsPressed = false;
		}

		public void Click(IUser user, Vector3 clickPoint)
		{
			_clickAction.StartAction(user, new ButtonData()
			{
				targetedPoints = new MWVector3[1] { clickPoint.CreateMWVector3() }
			});
		}

		internal ButtonBehaviorContext()
			: base()
		{
			
		}

		internal override void SynchronizeBehavior()
		{
			base.SynchronizeBehavior();

			if (_hoverPoints.Any())
			{
				_hoverAction.PerformActionUpdate(new ButtonData()
				{
					targetedPoints = _hoverPoints.ToArray()
				});

				_hoverPoints.Clear();
			}

			if (_buttonPressedPoints.Any())
			{
				_buttonAction.PerformActionUpdate(new ButtonData()
				{
					targetedPoints = _buttonPressedPoints.ToArray()
				});

				_buttonPressedPoints.Clear();
			}
		}

		protected override void OnTargetPointUpdated(Vector3 targetPoint)
		{
			if (IsPressed)
			{
				_buttonPressedPoints.Add(targetPoint.CreateMWVector3());
			}
			else
			{
				_hoverPoints.Add(targetPoint.CreateMWVector3());
			}
		}

		protected override void OnInitialized()
		{
			base.OnInitialized();
			RegisterActionHandler(_hoverAction, nameof(_hoverAction));
			RegisterActionHandler(_clickAction, nameof(_clickAction));
			RegisterActionHandler(_buttonAction, nameof(_buttonAction));
		}
	}
}
