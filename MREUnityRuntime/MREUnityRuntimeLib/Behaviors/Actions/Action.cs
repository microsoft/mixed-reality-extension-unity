// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
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
		Stopped
	}

	/// <summary>
	/// The event argument class to provide information about the action state change event.
	/// </summary>
	public sealed class ActionStateChangedArgs
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
	/// The class that serves as the basic actions that are a part of a behavior.
	/// </summary>
	public sealed class MWAction
	{
		private Dictionary<IUser, ActionState> _userActionStates = new Dictionary<IUser, ActionState>();

		internal IActionHandler Handler { get; set; }

		/// <summary>
		/// Signals the start of this action for the given user.
		/// </summary>
		/// <param name="user">The user starting the action.</param>
		public void StartAction(IUser user)
		{
			UpdateAction(user, ActionState.Started);
		}

		/// <summary>
		/// Signals the stop of this action for the given user.
		/// </summary>
		/// <param name="user">The user stopping the action.</param>
		public void StopAction(IUser user)
		{
			UpdateAction(user, ActionState.Stopped);
		}

		private void UpdateAction(IUser user, ActionState newState)
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

			Handler?.HandleActionStateChanged(user, oldState, newState);
		}
	}
}
