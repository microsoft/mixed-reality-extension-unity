// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.IPC;
using MixedRealityExtension.RPC;
using MixedRealityExtension.PluginInterfaces;
using System;
using UnityEngine;

namespace MixedRealityExtension.App
{
	/// <summary>
	/// Interface that represents the instance of an app running in the Mixed Reality Extension application.
	/// </summary>
	public interface IMixedRealityExtensionApp
	{
		/// <summary>
		/// Event that is raised when the mixed reality extension app has connected to the app.
		/// </summary>
		event MWEventHandler OnConnecting;

		/// <summary>
		/// Event that is raised when the attempt to connect to the mixed reality extension app fails.
		/// </summary>
		event MWEventHandler<ConnectFailedReason> OnConnectFailed;

		/// <summary>
		/// Event that is raised when the mixed reality extension app has connected to the app.
		/// </summary>
		event MWEventHandler OnConnected;

		/// <summary>
		/// Event that is raised when the mixed reality extension app has been disconnected from the app.
		/// </summary>
		event MWEventHandler OnDisconnected;

		/// <summary>
		/// Event that is raised when the mixed reality extension app has started up.
		/// </summary>
		event MWEventHandler OnAppStarted;

		/// <summary>
		/// Event that is raised when the mixed reality extension app has been shutdown.
		/// </summary>
		event MWEventHandler OnAppShutdown;

		/// <summary>
		/// Event that is raised when the app runtime has created a new actor.
		/// </summary>
		event MWEventHandler<IActor> OnActorCreated;

		/// <summary>
		/// Event that is raised when the local user joins the MRE application.
		/// </summary>
		event MWEventHandler<IUserInfo> OnUserJoined;

		/// <summary>
		/// Event that is raised when the local user leaves the MRE application.
		/// </summary>
		event MWEventHandler<IUserInfo> OnUserLeft;

		/// <summary>
		/// Gets the global id of the mixed reality extension app.
		/// </summary>
		string GlobalAppId { get; }

		/// <summary>
		/// Gets the session id of the mixed reality extension app.
		/// </summary>
		string SessionId { get; }

		/// <summary>
		/// Gets the local user
		/// </summary>
		IUser LocalUser { get; }

		/// <summary>
		/// Gets whether the mixed reality extension app has been started.
		/// </summary>
		bool IsActive { get; }

		/// <summary>
		/// The game object that serves as the scene root.
		/// </summary>
		GameObject SceneRoot { get; set; }

		/// <summary>
		/// The RPC interface for registering handlers and invoking remote procedure calls.
		/// </summary>
		RPCInterface RPC { get; }

		/// <summary>
		/// The RPC interface for registering channel handlers for invoking remote procedure calls.
		/// </summary>
		RPCChannelInterface RPCChannels { get; }

		/// <summary>
		/// Gets the logger to use within the MRE SDK.
		/// </summary>
		IMRELogger Logger { get; }

		/// <summary>
		/// Connect the mixed reality extension app to the given url with the given session id.
		/// <param name="url">The url to connect to for the app.</param>
		/// <param name="sessionId">The session id of the app.</param>
		/// <param name="platformId">Information about the host platform.</param>
		/// </summary>
		void Startup(string url, string sessionId, string platformId);

		/// <summary>
		/// Called to shut down the engine mixed reality extension app by the app process.
		/// </summary>
		void Shutdown();

		/// <summary>
		/// Update the remote app runtime.
		/// </summary>
		void Update();

		/// <summary>
		/// User is joining the app.
		/// </summary>
		/// <param name="userGO">The game object that serves as the user in unity.</param>
		/// <param name="userInfo">Interface for providing information about the user.</param>
		void UserJoin(GameObject userGO, IUserInfo userInfo);

		/// <summary>
		/// User is leaving the app.
		/// </summary>
		/// <param name="userGO">The game object that serves as the user in unity.</param>
		void UserLeave(GameObject userGO);

		/// <summary>
		/// Enable user interaction with the app.
		/// </summary>
		/// <param name="user">The user to enable interaction for.</param>
		void EnableUserInteraction(IUser user);

		/// <summary>
		/// Disable user interaction with the app.
		/// </summary>
		/// <param name="user">The user to disable interaction for.</param>
		void DisableUserInteration(IUser user);

		/// <summary>
		/// Find an actor with the given id or null if none exists.
		/// </summary>
		/// <param name="id">The id of the actor.</param>
		/// <returns>The actor with the given id in this app or null if none exists.</returns>
		IActor FindActor(Guid id);

		/// <summary>
		/// Callback from the engine when an actor is being destroyed from within the engine runtime.
		/// </summary>
		/// <param name="actorId"></param>
		void OnActorDestroyed(Guid actorId);
	}
}
