// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;

namespace MixedRealityExtension.Behaviors.Handlers
{
	internal abstract class ToolHandler<ToolDataT> : BehaviorHandlerBase
		where ToolDataT : BaseToolData, new()
	{
		private readonly IToolBehavior<ToolDataT> _toolBehavior;

		private ToolDataT _queuedToolData;

		internal ToolDataT ToolData { get; private set; }

		internal bool IsUsing { get; private set; }

		internal ToolHandler(IToolBehavior<ToolDataT> tool, WeakReference<MixedRealityExtensionApp> appRef, IActor attachedActor)
			: base(tool, appRef, attachedActor)
		{
			RegisterActionHandler(tool.Holding, nameof(tool.Holding));
			RegisterActionHandler(tool.Using, nameof(tool.Using));

			_toolBehavior = tool;
			_toolBehavior.Using.ActionStateChanging += OnUsingStateChanging;

			ToolData = new ToolDataT();
		}

		protected override sealed void SynchronizeBehavior()
		{
			if (IsUsing)
			{
				PerformUsingAction();
			}
		}

		protected override void CleanUp()
		{
			// Clean up our action listeners first then let the base handler cleanup the behavior.
			_toolBehavior.Using.ActionStateChanging -= OnUsingStateChanging;
			base.CleanUp();
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
				_toolBehavior.Using.PerformActionUpdate(_queuedToolData);
			}
		}
	}
}
