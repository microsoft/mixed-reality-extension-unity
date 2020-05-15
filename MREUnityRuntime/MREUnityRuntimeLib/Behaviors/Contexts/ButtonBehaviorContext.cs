using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;
using UnityEngine;

namespace MixedRealityExtension.Behaviors.Contexts
{
	public class ButtonBehaviorContext : TargetBehaviorContext
	{
		private MWAction _hoverAction = new MWAction();
		private MWAction _clickAction = new MWAction();
		private MWAction _buttonAction = new MWAction();

		public void StartHover(IUser user, Vector3 hoverPoint)
		{

		}

		public void EndHover(IUser user, Vector3 hoverPoint)
		{

		}

		public void StartButton(IUser user, Vector3 buttonStartPoint)
		{

		}

		public void EndButton(IUser user, Vector3 buttonEndPoint)
		{

		}

		public void Click(IUser user, Vector3 clickPoint)
		{

		}

		internal ButtonBehaviorContext()
			: base()
		{
			RegisterActionHandler(_hoverAction, nameof(_hoverAction));
			RegisterActionHandler(_clickAction, nameof(_clickAction));
			RegisterActionHandler(_buttonAction, nameof(_buttonAction));
		}
	}
}
