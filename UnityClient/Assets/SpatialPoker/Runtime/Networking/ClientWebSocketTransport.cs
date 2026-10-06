using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SpatialPoker.Networking
{
    public sealed class ClientWebSocketTransport : MonoBehaviour, IGameTransport
    {
        private const string UrlPrefKey = "poker_server_url";

        [SerializeField] private string url = "ws://127.0.0.1:8080";

        private readonly ConcurrentQueue<Action> _mainThread = new();
        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;

        public bool IsConnected =>
            _socket != null && _socket.State == WebSocketState.Open;

        public event Action Connected;
        public event Action Disconnected;
        public event Action<string> MessageReceived;

        /// <summary>Effective server URL (PlayerPrefs override wins).</summary>
        public string CurrentUrl => url;

        /// <summary>
        /// Points the client at a new server URL. Phone builds can't reach
        /// 127.0.0.1, so the HUD exposes this; the choice persists.
        /// </summary>
        public void SetUrl(string newUrl)
        {
            if (string.IsNullOrWhiteSpace(newUrl))
                return;

            newUrl = newUrl.Trim();
            if (!newUrl.StartsWith("ws://") && !newUrl.StartsWith("wss://"))
                newUrl = "ws://" + newUrl;

            url = newUrl;
            PlayerPrefs.SetString(UrlPrefKey, url);
            PlayerPrefs.Save();
        }

        private void Awake()
        {
            // A phone that previously connected to a LAN server keeps using it.
            if (PlayerPrefs.HasKey(UrlPrefKey))
            {
                var saved = PlayerPrefs.GetString(UrlPrefKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(saved))
                    url = saved.Trim();
            }
        }

        private void Update()
        {
            while (_mainThread.TryDequeue(out var action))
                action?.Invoke();
        }

        public void Connect()
        {
            _ = ConnectAsync();
        }

        public void Disconnect()
        {
            _ = DisconnectAsync();
        }

        public void Send(string json)
        {
            if (!IsConnected || string.IsNullOrEmpty(json))
                return;

            _ = SendAsync(json);
        }

        private async Task ConnectAsync()
        {
            if (IsConnected)
                return;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _socket?.Dispose();
            _socket = new ClientWebSocket();

            try
            {
                await _socket.ConnectAsync(new Uri(url), _cts.Token);
                _mainThread.Enqueue(() => Connected?.Invoke());
                _ = ReceiveLoopAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpatialPoker] WebSocket connect failed: {ex.Message}", this);
                QueueDisconnected();
            }
        }

        private async Task SendAsync(string json)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                await _socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SpatialPoker] WebSocket send failed: {ex.Message}", this);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            var buffer = new byte[8192];
            var builder = new StringBuilder();

            try
            {
                while (!token.IsCancellationRequested &&
                       _socket != null &&
                       _socket.State == WebSocketState.Open)
                {
                    builder.Clear();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await _socket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            token);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await DisconnectAsync();
                            return;
                        }

                        builder.Append(
                            Encoding.UTF8.GetString(
                                buffer,
                                0,
                                result.Count));
                    }
                    while (!result.EndOfMessage);

                    var message = builder.ToString();
                    _mainThread.Enqueue(() => MessageReceived?.Invoke(message));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SpatialPoker] WebSocket receive stopped: {ex.Message}", this);
            }
            finally
            {
                QueueDisconnected();
            }
        }

        private async Task DisconnectAsync()
        {
            try
            {
                _cts?.Cancel();

                if (_socket != null &&
                    (_socket.State == WebSocketState.Open ||
                     _socket.State == WebSocketState.CloseReceived))
                {
                    await _socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "client disconnect",
                        CancellationToken.None);
                }
            }
            catch
            {
            }
            finally
            {
                _socket?.Dispose();
                _socket = null;
                QueueDisconnected();
            }
        }

        private void QueueDisconnected()
        {
            _mainThread.Enqueue(() => Disconnected?.Invoke());
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _socket?.Dispose();
            _cts?.Dispose();
        }
    }
}
