// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace MixedRealityExtension.IPC.Connections
{
	/// <summary>
	/// Class representing a WebSocket connection.
	/// </summary>
	public class WebSocket : IConnectionInternal
	{
		private const int ReceiveBufferSize = 8192;

		private volatile ClientWebSocket _ws;
		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken CancellationToken => _cancellationTokenSource.Token;
		private BlockingCollection<Action> _eventQueue = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
		private BlockingCollection<Func<Task>> _sendQueue = new BlockingCollection<Func<Task>>(new ConcurrentQueue<Func<Task>>());
		private Task _workerTask;

		/// <inheritdoc />
		public event MWEventHandler OnConnecting;

		/// <inheritdoc />
		public event MWEventHandler<ConnectFailedReason> OnConnectFailed;

		/// <inheritdoc />
		public event MWEventHandler OnConnected;

		/// <inheritdoc />
		public event MWEventHandler OnDisconnected;

		/// <inheritdoc />
		public event MWEventHandler<Exception> OnError;

		/// <inheritdoc />
		public event MWEventHandler<Message> OnReceive;

		/// <inheritdoc />
		public bool IsActive => _cancellationTokenSource != null;

		/// <summary>
		/// Gets or sets the Url this WebSocket should connect to.
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// Gets or sets the headers that will be sent in the connect request.
		/// </summary>
		public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();

		/// <inheritdoc />
		public void Open()
		{
			if (disposed)
			{
				throw new ObjectDisposedException("WebSocket has been disposed");
			}

			if (IsActive)
			{
				throw new InvalidOperationException("WebSocket already initialized");
			}

			StartWorker();
		}

		/// <inheritdoc />
		public void Close()
		{
			if (disposed)
			{
				throw new ObjectDisposedException("WebSocket has been disposed");
			}

			if (IsActive)
			{
				Task.Run(() => CloseInternal(true, "Closed locally")).Wait();
				Task.Run(() => DisposeInternal()).Wait();
			}
		}

		private async Task CloseInternal(bool clean, string reason)
		{
			if (clean)
			{
				try { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken).ConfigureAwait(false); } catch { }
			}
			else
			{
				try { await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken).ConfigureAwait(false); } catch { }
			}
		}

		/// <inheritdoc />
		public void Send(Message message)
		{
			if (disposed)
			{
				throw new ObjectDisposedException("WebSocket has been disposed");
			}

			if (_ws.State != WebSocketState.Open)
			{
				throw new InvalidOperationException("WebSocket is not open");
			}

			// Ensure the message is sent on this websocket rather than a websocket allocated in the future (in case of a reconnect).
			var ws = _ws;

			_sendQueue.Add(async () =>
			{
				try
				{
					var json = JsonConvert.SerializeObject(message, Constants.SerializerSettings);
					var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
					await ws.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken).ConfigureAwait(false);
				}
				catch (Exception)
				{
					// TODO: Log the exception, once the global MREAPI logger exists.
				}
			});
		}

		private async Task Connect()
		{
			ConnectFailedReason? connectFailedReason = null;

			var parsedUri = new Uri(Url);

			if (_ws != null)
			{
				DisposeWebSocket();
			}

			_ws = new ClientWebSocket();
			_ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(60);
			foreach (var item in Headers)
			{
				_ws.Options.SetRequestHeader(item.Key, UnityWebRequest.EscapeURL(item.Value));
			}

			try
			{
				await _ws.ConnectAsync(parsedUri, CancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{ }
			catch (WebSocketException e)
			{
				connectFailedReason = ConnectFailedReason.ConnectionFailed;

				// Try to determine if the connection failed due to unsupported protocol version.
				// This is a workaround for a shortcoming in .Net's ClientWebSocket implementation:
				//     "ClientWebSocket does not provide upgrade request error details"
				//      https://github.com/dotnet/corefx/issues/29163
				// As a workaround the server returns an unexpected subprotocol, resulting in a
				// WebSocketError.UnsupportedProtocol error code.

				if (e.WebSocketErrorCode == WebSocketError.UnsupportedProtocol)
				{
					connectFailedReason = ConnectFailedReason.UnsupportedProtocol;
				}
			}
			catch (Exception)
			{
				connectFailedReason = ConnectFailedReason.ConnectionFailed;
			}

			// If the connect attempt failed, raise the ConnectFailed event.
			if (connectFailedReason.HasValue)
			{
				Invoke_OnConnectFailed(connectFailedReason.Value);
			}

			// If the connection was refused due to Unsupported Protocol, then shutdown the websocket.
			if (connectFailedReason == ConnectFailedReason.UnsupportedProtocol)
			{
				_cancellationTokenSource.Cancel();
			}
		}

		private async Task ReadTask()
		{
			try
			{
				var wasOpen = false;
				var stream = new MemoryStream();
				var receiveBuffer = new ArraySegment<byte>(new byte[ReceiveBufferSize], 0, ReceiveBufferSize);
				WebSocketReceiveResult result = default;
				CancellationTokenSource sendWorkerCancellationSource = default;
				Task sendWorker = default;

				while (true)
				{
					try
					{
						// Exit task if requested.
						CancellationToken.ThrowIfCancellationRequested();

						// Notify host the we're attempting to establish the connection.
						Invoke_OnConnecting();

						// Start connecting.
						wasOpen = false;
						await Connect().ConfigureAwait(false);

						 // Wait until the connection is fully resolved (whether that be success or failure).
						while (_ws.State == WebSocketState.Connecting)
						{
							// Exit task if requested.
							CancellationToken.ThrowIfCancellationRequested();
							// Pause for a very short period (polling for state change).
							await Task.Delay(20, CancellationToken).ConfigureAwait(false);
						}

						// If connection failed, retry after a short delay.
						if (_ws.State != WebSocketState.Open)
						{
							await Task.Delay(1000, CancellationToken).ConfigureAwait(false);
							continue;
						}

						// Connection has successfully established.
						wasOpen = true;

						// Once connected, start the send worker.
						sendWorkerCancellationSource = new CancellationTokenSource();
						var sendWorkerCancellationToken = sendWorkerCancellationSource.Token;
						sendWorker = Task.Run(async () =>
						{
							while (true)
							{
								try
								{
									sendWorkerCancellationSource.Token.ThrowIfCancellationRequested();

									if (_sendQueue.TryTake(out Func<Task> send, -1, sendWorkerCancellationToken))
									{
										await send().ConfigureAwait(false);
									}
								}
								catch
								{
									break;
								}
							}
						}, sendWorkerCancellationToken);

						// Notify host the connection was successfully established (it's important to do this after send worker startup).
						Invoke_OnConnected();

						while (_ws.State == WebSocketState.Open)
						{
							// Read a complete message.
							do
							{
								result = await _ws.ReceiveAsync(receiveBuffer, CancellationToken).ConfigureAwait(false);
								if (result != null)
								{
									if (result.MessageType == WebSocketMessageType.Close)
									{
										await CloseInternal(false, "Closed remotely").ConfigureAwait(false);
									}
									else if (result.MessageType == WebSocketMessageType.Text)
									{
										stream.Write(receiveBuffer.Array, 0, result.Count);
									}
									else
									{
										throw new NotSupportedException($"Unsupported WebSocketMessageType: {result.MessageType}");
									}
								}

								// Exit task if requested.
								CancellationToken.ThrowIfCancellationRequested();
							}
							while (result != null && !result.EndOfMessage);

							// Dispatch the message.
							if (result != null && result.EndOfMessage && stream.Length > 0)
							{
								var json = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
								Invoke_OnReceive(json);
							}

							// Reset accumulation buffer.
							stream.SetLength(0);
						}
					}
					catch (OperationCanceledException)
					{ }
					catch (Exception e)
					{
						Invoke_OnError(e);
					}

					if (wasOpen)
					{
						// Shutdown the send worker.
						sendWorkerCancellationSource.Cancel();
						await sendWorker.ConfigureAwait(false);
						sendWorkerCancellationSource.Dispose();

						// Notify host the connection was closed.
						Invoke_OnDisconnected();
					}

					// Exit task if requested.
					CancellationToken.ThrowIfCancellationRequested();

					// Attempt to reconnect after a short delay.
					await Task.Delay(1000, CancellationToken).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException)
			{ }
			catch (Exception e)
			{
				Invoke_OnError(e);
			}
		}

		void IConnectionInternal.Update()
		{
			while (_eventQueue.TryTake(out Action action))
			{
				try { action.Invoke(); } catch { }
			}
		}

		private void Invoke_OnConnecting()
		{
			_eventQueue.Add(() => OnConnecting?.Invoke());
		}

		private void Invoke_OnConnectFailed(ConnectFailedReason reason)
		{
			_eventQueue.Add(() => OnConnectFailed?.Invoke(reason));
		}

		private void Invoke_OnConnected()
		{
			_eventQueue.Add(() => OnConnected?.Invoke());
		}

		private void Invoke_OnDisconnected()
		{
			_eventQueue.Add(() => OnDisconnected?.Invoke());
		}

		private void Invoke_OnError(Exception e)
		{
			_eventQueue.Add(() => OnError?.Invoke(e));
		}

		private void Invoke_OnReceive(string json)
		{
			// TODO: verbose log the message, once MREAPI global logger exists
			// if (MREAPI.AppsAPI.VerboseLogging)
			// {
			// 	MREAPI.Logger.LogDebug($"Recv: {json}");
			// }

			try
			{
				Message message = JsonConvert.DeserializeObject<Message>(json, Constants.SerializerSettings);
				_eventQueue.Add(() => OnReceive?.Invoke(message));
			}
			catch (Exception)
			{
				// TODO: Log the error, once MREAPI global logger exists
				// MREAPI.Logger.LogDebug($"Error deserializing message.Json: {json}\nException: {e.Message}\nStackTrace: {e.StackTrace}");
			}
		}

		private void StartWorker()
		{
			_cancellationTokenSource = new CancellationTokenSource();
			_workerTask = Task.Run(ReadTask, CancellationToken);
		}

		#region IDisposable Support
		private bool disposed = false;

		private async Task DisposeWorker()
		{
			// Best effort with no exceptions.
			try { _cancellationTokenSource.Cancel(); } catch { }
			try { await _workerTask.ConfigureAwait(false); } catch { }
			try { _cancellationTokenSource.Dispose(); } catch { }

			_workerTask = null;
			_cancellationTokenSource = null;
		}

		private void DisposeWebSocket()
		{
			if (_ws != null)
			{
				var ws = _ws;
				_ws = null;
				// Best effort with no exceptions.
				try { ws.Dispose(); } catch { }
			}
		}

		private async Task DisposeInternal()
		{
			await DisposeWorker().ConfigureAwait(false);
			DisposeWebSocket();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
				Task.Run(() => CloseInternal(true, "Dispose")).Wait();
				Task.Run(() => DisposeInternal()).Wait();
			}
		}
		#endregion
	}
}
