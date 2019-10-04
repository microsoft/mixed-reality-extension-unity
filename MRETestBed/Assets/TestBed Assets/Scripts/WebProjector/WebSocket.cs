// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace AltspaceVR.WebProjector
{
	public class WebSocket
	{
		private const int ReceiveChunkSize = 1024;
		private const int SendChunkSize = 1024;

		private ClientWebSocket ws;
		private System.Threading.CancellationTokenSource cancellationTokenSource;
		private readonly ConcurrentQueue<Action> mainThreadWorkQueue = new ConcurrentQueue<Action>();
		private readonly ConcurrentQueue<Action> onConnectWorkQueue = new ConcurrentQueue<Action>();
		private Dictionary<string, string> headers;
		private Uri uri;

		public event Action OnConnected;
		public event Action<string> OnMessage;

		public WebSocket(string url, Dictionary<string, string> headers = null)
		{
			uri = new Uri(url);
			this.headers = new Dictionary<string, string>(headers);
		}

		public void Update()
		{
			Action action;
			while (mainThreadWorkQueue.TryDequeue(out action))
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					Debug.LogError(ex);
				}
			}
		}

		public void Close()
		{
			if (cancellationTokenSource != null)
			{
				cancellationTokenSource.Cancel();
			}
		}

		private void DisposeWebSocket()
		{
			if (ws != null)
			{
				try { ws.Dispose(); } catch { }
				ws = null;
			}
		}

		public async void StartConnecting()
		{
			if (ws != null)
			{
				throw new InvalidOperationException("WebSocket already exists.");
			}

			Debug.Log($"Connecting to {uri}.");

			cancellationTokenSource = new System.Threading.CancellationTokenSource();
			ws = new ClientWebSocket();
			ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
			foreach (var headerKV in headers)
			{
				ws.Options.SetRequestHeader(headerKV.Key, headerKV.Value);
			}

			try
			{
				await ws.ConnectAsync(uri, cancellationTokenSource.Token);
				mainThreadWorkQueue.Enqueue(() => WS_OnConnected());
				return;
			}
			catch (OperationCanceledException)
			{
				DisposeWebSocket();
				return;
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
			}

			mainThreadWorkQueue.Enqueue(() => DelayReconnect());
		}

		private async void StartReceiving()
		{
			var buffer = new byte[ReceiveChunkSize];

			try
			{
				while (true)
				{
					cancellationTokenSource.Token.ThrowIfCancellationRequested();

					var stringResult = new StringBuilder();

					if (ws.State == WebSocketState.Open)
					{
						WebSocketReceiveResult result;
						do
						{
							cancellationTokenSource.Token.ThrowIfCancellationRequested();

							result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);

							if (result.MessageType == WebSocketMessageType.Close)
							{
								try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, System.Threading.CancellationToken.None); } catch { }
								mainThreadWorkQueue.Enqueue(() => DelayReconnect());
							}
							else
							{
								var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
								stringResult.Append(str);
							}
						} while (!result.EndOfMessage);

						string json = stringResult.ToString();
						mainThreadWorkQueue.Enqueue(() => _OnMessage(json));
						stringResult.Clear();
					}
					else
					{
						stringResult.Clear();
						await Task.Delay(1000);
					}
				}
			}
			catch (OperationCanceledException)
			{
				try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, System.Threading.CancellationToken.None); } catch { }
				DisposeWebSocket();
				return;
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
			}

			mainThreadWorkQueue.Enqueue(() => DelayReconnect());
		}

		private async void DelayReconnect()
		{
			await Task.Delay(1000 + UnityEngine.Random.Range(-500, 500));
			DisposeWebSocket();
			StartConnecting();
		}

		public Task Send(object message)
		{
			var json = JsonConvert.SerializeObject(message);
			return Send(json);
		}

		public async Task Send(string message)
		{
			Debug.Log($"Send {message}");

			var data = Encoding.UTF8.GetBytes(message);

			Func<Task> DoSend = async () =>
			{
				var messagesCount = (int)Math.Ceiling((double)data.Length / SendChunkSize);

				for (var i = 0; i < messagesCount; i++)
				{
					var offset = SendChunkSize * i;
					var count = SendChunkSize;
					var lastMessage = ((i + 1) == messagesCount);

					if ((count * (i + 1)) > data.Length)
					{
						count = data.Length - offset;
					}

					await ws.SendAsync(new ArraySegment<byte>(data, offset, count), WebSocketMessageType.Text, lastMessage, cancellationTokenSource.Token);
				}
			};

			if (ws == null)
			{
				throw new InvalidOperationException("ERR WebSocket is closed.");
			}

			if (ws.State != WebSocketState.Open)
			{
				// onConnectWorkQueue.Enqueue(async () => await DoSend());
				throw new InvalidOperationException("ERR WebSocket not open yet.");
			}
			else
			{
				await DoSend();
			}
		}

		private void WS_OnConnected()
		{
			try
			{
				OnConnected?.Invoke();
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
			}

			Action action;
			while (onConnectWorkQueue.TryDequeue(out action))
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					Debug.LogError(ex);
				}
			}
			StartReceiving();
		}

		private void _OnMessage(string message)
		{
			Debug.Log($"Recv {message}");
			try
			{
				OnMessage?.Invoke(message);
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
			}
		}
	}
}
