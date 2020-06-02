// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;

namespace MixedRealityExtension.Behaviors.Contexts
{
	public abstract class PhysicalToolBehaviorContext<ToolDataT> : TargetBehaviorContext
		where ToolDataT : BaseToolData, new()
	{
		private ToolDataT _queuedToolData;
		private MWAction<ToolDataT> _holding = new MWAction<ToolDataT>();
		private MWAction<ToolDataT> _using = new MWAction<ToolDataT>();

		public void StartHolding(IUser user)
		{
			_holding.StartAction(user);
		}

		public void EndHolding(IUser user)
		{
			_holding.StopAction(user);
		}

		public void StartUsing(IUser user)
		{
			IsUsing = true;
			_using.StartAction(user);
		}

		public void EndUsing(IUser user)
		{
			IsUsing = false;
			_using.StopAction(user);
		}

		internal ToolDataT ToolData { get; private set; }

		internal bool IsUsing { get; private set; }


		internal PhysicalToolBehaviorContext()
		{
			ToolData = new ToolDataT();
		}

		internal override sealed void SynchronizeBehavior()
		{
			if (IsUsing)
			{
				PerformUsingAction();
			}
		}

		protected override void OnInitialized()
		{
			RegisterActionHandler(_holding, "holding");
			RegisterActionHandler(_using, "using");
		}

		private void OnUsingStateChanging(object sender, ActionStateChangedArgs args)
		{
			var wasUsing = IsUsing;
			IsUsing = args.NewState == ActionState.Started || args.NewState == ActionState.Performing;
			if (!IsUsing && wasUsing)
			{
				// We are stopping use and should send the remaining tool data up the remaining tool data from the last bit of use.
				PerformUsingAction();
			}
		}

		private void PerformUsingAction()
		{
			if (!ToolData.IsEmpty)
			{
				_queuedToolData = ToolData;
				ToolData = new ToolDataT();
				_using.PerformActionUpdate(_queuedToolData);
			}
		}
	}
}
