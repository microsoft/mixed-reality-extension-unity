// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Messaging;
using System;

namespace MixedRealityExtension.IPC
{
	/// <summary>
	/// Event handler type with no arguments.
	/// </summary>
	public delegate void MWEventHandler();

	/// <summary>
	/// Event handler type with an argument.
	/// </summary>
	/// <typeparam name="ArgsT"></typeparam>
	/// <param name="args"></param>
	public delegate void MWEventHandler<ArgsT>(ArgsT args);

	/// <summary>
	/// Event handler type with two arguments.
	/// </summary>
	/// <typeparam name="ArgsT"></typeparam>
	/// <typeparam name="ArgsU"></typeparam>
	/// <param name="arg1"></param>
	/// <param name="arg2"></param>
	public delegate void MWEventHandler<ArgsT, ArgsU>(ArgsT arg1, ArgsU arg2);

	/// <summary>
	/// The set of possible reason codes passed to the OnConnectFailed event.
	/// </summary>
	public enum ConnectFailedReason
	{
		/// <summary>
		/// The connection failed to establish.
		/// </summary>
		ConnectionFailed,

		/// <summary>
		/// The connection was refused due to a protocol version mismatch.
		/// </summary>
		UnsupportedProtocol
	}

	/// <summary>
	/// Interface representing a connection.
	/// </summary>
	public interface IConnection : IDisposable
	{
		/// <summary>
		/// Invoked before the connection attempt is initiated.
		/// </summary>
		event MWEventHandler OnConnecting;

		/// <summary>
		/// Invoked after the connection attempt failed.
		/// </summary>
		event MWEventHandler<ConnectFailedReason> OnConnectFailed;

		/// <summary>
		/// Invoked after the connection attempt succeeds.
		/// </summary>
		event MWEventHandler OnConnected;

		/// <summary>
		/// Invoked after the connection has been closed.
		/// </summary>
		event MWEventHandler OnDisconnected;

		/// <summary>
		/// Invoked after the connection receives a message;
		/// </summary>
		event MWEventHandler<Message> OnReceive;

		/// <summary>
		/// Invoked when an error occurred on the connection.
		/// </summary>
		event MWEventHandler<Exception> OnError;

		/// <summary>
		/// Returns true if the connection is in an active state (i.e. if Open has been called).
		/// </summary>
		bool IsActive { get; }

		/// <summary>
		/// Opens the connection.
		/// </summary>
		void Open();

		/// <summary>
		/// Closes the connection.
		/// </summary>
		void Close();

		/// <summary>
		/// Sends a message to the remote endpoint.
		/// </summary>
		/// <param name="message">The message to send.</param>
		void Send(Message message);
	}

	internal interface IConnectionInternal : IConnection
	{
		void Update();
	}
}
