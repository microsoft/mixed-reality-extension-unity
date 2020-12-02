// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Behaviors.Actions
{
	/// <summary>
	/// The state of the action.
	/// </summary>
	public enum ActionState
	{
		/// <summary>
		/// The action is started.
		/// </summary>
		Started,

		/// <summary>
		/// The action is stopped.
		/// </summary>
		Stopped,

		/// <summary>
		/// The action is currently being performed.
		/// </summary>
		Performing
	}

	/// <summary>
	/// The event argument class to provide information about the action state change event.
	/// </summary>
	internal sealed class ActionStateChangedArgs
	{
		/// <summary>
		/// The id of the user that has cause a state change for the action.
		/// </summary>
		public Guid UserId { get; }

		/// <summary>
		/// The old state of the action.
		/// </summary>
		public ActionState OldState { get; }

		/// <summary>
		/// The new state of the action.
		/// </summary>
		public ActionState NewState { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ActionStateChangedArgs"/> class.
		/// </summary>
		/// <param name="userId">The id of the user causing the state change.</param>
		/// <param name="oldState">The old state of the action.</param>
		/// <param name="newState">The new state of the action.</param>
		public ActionStateChangedArgs(Guid userId, ActionState oldState, ActionState newState)
		{
			UserId = userId;
			OldState = oldState;
			NewState = newState;
		}
	}

	/// <summary>
	/// Abstract base class for an action within the MRE behavior system.
	/// </summary>
	public abstract class MWActionBase
	{
		private Dictionary<IUser, ActionState> _userActionStates = new Dictionary<IUser, ActionState>();

		internal BehaviorActionHandler Handler { get; set; }

		internal EventHandler<ActionStateChangedArgs> ActionStateChanging { get; set; }

		internal EventHandler<ActionStateChangedArgs> ActionStateChanged { get; set; }

		protected void ChangeState(IUser user, ActionState newState, BaseActionData actionData)
		{
			if (user == null)
			{
				throw new ArgumentNullException("User cannot be null when performing an action");
			}

			ActionState oldState = ActionState.Stopped;
			if (_userActionStates.TryGetValue(user, out oldState))
			{
				_userActionStates[user] = newState;
			}
			else
			{
				_userActionStates.Add(user, newState);
			}

			ActionStateChanging?.Invoke(this, new ActionStateChangedArgs(user.Id, oldState, newState));
			Handler?.HandleActionStateChanged(user, oldState, newState, actionData);
			ActionStateChanged?.Invoke(this, new ActionStateChangedArgs(user.Id, oldState, newState));
		}

		protected void UpdateAction(BaseActionData actionData)
		{
			foreach (var userActionState in _userActionStates)
			{
				if (userActionState.Value != ActionState.Stopped)
				{
					Handler?.HandleActionPerforming(userActionState.Key, actionData);
				}
			}
		}
	}

	/// <summary>
	/// The class that serves as the basic actions that are a part of a behavior.
	/// </summary>
	public sealed class MWAction : MWActionBase
	{
		/// <summary>
		/// Signals the start of this action for the given user.
		/// </summary>
		/// <param name="user">The user starting the action.</param>
		public void StartAction(IUser user)
		{
			ChangeState(user, ActionState.Started, null);
		}

		/// <summary>
		/// Signals the stop of this action for the given user.
		/// </summary>
		/// <param name="user">The user stopping the action.</param>
		public void StopAction(IUser user)
		{
			ChangeState(user, ActionState.Stopped, null);
		}

		/// <summary>
		/// Provides an action update while the action is being performed.
		/// </summary>
		public void PerformActionUpdate()
		{
			UpdateAction(null);
		}
	}

	/// <summary>
	/// The class that serves as the basic actions with action data that are a part of a behavior.
	/// </summary>
	/// <typeparam name="ActionDataT">The action data type associated with the action events.</typeparam>
	public sealed class MWAction<ActionDataT> : MWActionBase
		where ActionDataT : BaseActionData
	{
		/// <summary>
		/// Signals the start of this action for the given user.
		/// </summary>
		/// <param name="user">The user starting the action.</param>
		/// <param name="actionData">The optional data to pass along.</param>
		public void StartAction(IUser user, ActionDataT actionData = null)
		{
			ChangeState(user, ActionState.Started, actionData);
		}

		/// <summary>
		/// Signals the stop of this action for the given user.
		/// </summary>
		/// <param name="user">The user stopping the action.</param>
		/// <param name="actionData">The optional data to pass along.</param>
		public void StopAction(IUser user, ActionDataT actionData = null)
		{
			ChangeState(user, ActionState.Stopped, actionData);
		}

		/// <summary>
		/// Provides an action update while the action is being performed.
		/// </summary>
		/// <param name="actionData">The optional data to pass along.</param>
		public void PerformActionUpdate(ActionDataT actionData = null)
		{
			UpdateAction(actionData);
		}
	}
}
