// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Assets;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.IPC;
using MixedRealityExtension.RPC;
using MixedRealityExtension.PluginInterfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedRealityExtension.App
{
	/// <summary>
	/// Interface that represents the instance of an app running in the Mixed Reality Extension application.
	/// </summary>
	public interface IMixedRealityExtensionApp
	{
		/// <summary>
		/// Event that is raised just before this app makes its permissions request to the <see cref="IPermissionManager"/>.
		/// </summary>
		event MWEventHandler OnWaitingForPermission;

		/// <summary>
		/// Event that is raised when a permission required to run the app is denied by the host. Will be immediately
		/// followed by <see cref="OnAppShutdown"/>.
		/// </summary>
		event MWEventHandler OnPermissionDenied;

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
		/// Event that is raised when the local user joins the MRE application. Is passed the user and a boolean
		/// indicating whether the user joined from the local client, or from a remote client.
		/// </summary>
		event MWEventHandler<IUser, bool> OnUserJoined;

		/// <summary>
		/// Event that is raised when the local user leaves the MRE application. Is passed the user and a boolean
		/// indicating whether the user left from the local client, or from a remote client.
		/// </summary>
		event MWEventHandler<IUser, bool> OnUserLeft;

		/// <summary>
		/// A string uniquely identifying the MRE behind the server URL. Used for generating consistent user IDs when
		/// user tracking is enabled.
		/// </summary>
		string GlobalAppId { get; }

		/// <summary>
		/// A string uniquely identifying the MRE instance in the shared space across all clients. Used for generating
		/// user IDs when user tracking is disabled.
		/// </summary>
		string EphemeralAppId { get; }

		/// <summary>
		/// Gets the session id of the mixed reality extension app.
		/// </summary>
		string SessionId { get; }

		/// <summary>
		/// Gets the local user. Will be null if the local client has not joined as a user.
		/// </summary>
		IUser LocalUser { get; }

		/// <summary>
		/// Gets whether the mixed reality extension app has been started.
		/// </summary>
		bool IsActive { get; }

		/// <summary>
		/// The URI of the MRE server. Only valid after `Startup` has been called.
		/// </summary>
		Uri ServerUri { get; }

		/// <summary>
		/// The game object that serves as the scene root.
		/// </summary>
		GameObject SceneRoot { get; set; }

		/// <summary>
		/// Where assets for this app instance are stored
		/// </summary>
		AssetManager AssetManager { get; }

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
		/// </summary>
		void Startup(string url, string sessionId);

		/// <summary>
		/// Called to shut down the engine mixed reality extension app by the app process.
		/// </summary>
		void Shutdown();

		/// <summary>
		/// Update keyframed rigid bodies.
		/// </summary>
		void FixedUpdate();

		/// <summary>
		/// Update the remote app runtime.
		/// </summary>
		void Update();

		/// <summary>
		/// User is joining the app.
		/// </summary>
		/// <param name="userGO">The game object that serves as the user in unity.</param>
		/// <param name="hostAppUser">Interface for providing a representation of the host app user.</param>
		/// <param name="isLocalUser">Indicates whether this user originates on this client, or is a local representation of a remote user.</param>
		void UserJoin(GameObject userGO, IHostAppUser hostAppUser, bool isLocalUser);

		/// <summary>
		/// User is leaving the app.
		/// </summary>
		/// <param name="userGO">The game object that serves as the user in unity.</param>
		void UserLeave(GameObject userGO);

		/// <summary>
		/// Gets whether this app is interactable for the given user.
		/// </summary>
		/// <param name="user">The user to check interactability for.</param>
		/// <returns>Whether the app is interactable for the given user.</returns>
		bool IsInteractableForUser(IUser user);

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

		/// <summary>
		/// Declare pre-allocated game objects as MRE actors. Note: Since these actors are not created via an MRE message, the app has
		/// no means to create them on clients that have not preallocated them. Thus cross-host compatibility will be reduced for these actors.
		/// </summary>
		/// <param name="objects">An array of GameObjects that this MRE should be aware of. GameObjects cannot already be owned by an MRE.</param>
		/// <param name="guidSeed">The seed for generating the new actors' IDs. Must be the same value across all clients
		/// in the session for this batch of actors, or the preallocated actors will not synchronize correctly. Must be unique for this MRE session.
		/// </param>
		/// <exception cref="Exception">Thrown when the app is not in the Started state.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when the value of the guidSeed argument has already been used this session.</exception>
		void DeclarePreallocatedActors(GameObject[] objects, string guidSeed);
	}
}
