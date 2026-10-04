using System;
using UnityEngine;
using SpatialPoker.Networking.Protocol;

namespace SpatialPoker.Networking
{
    public sealed class GameStateSynchronizer : MonoBehaviour
    {
        [SerializeField] private string roomCode;
        [SerializeField] private string playerId;

        private IGameTransport _transport;

        public GameStateStore Store { get; } = new();

        public event Action ResyncRequested;

        public void Bind(IGameTransport transport)
        {
            if (_transport != null)
                _transport.MessageReceived -= OnMessage;

            _transport = transport;

            if (_transport != null)
                _transport.MessageReceived += OnMessage;
        }

        private void OnDestroy()
        {
            if (_transport != null)
                _transport.MessageReceived -= OnMessage;
        }

        private void OnMessage(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            var type = ExtractType(json);

            if (type == "GAME_SNAPSHOT")
            {
                var envelope = JsonUtility.FromJson<GameSnapshotEnvelopeDto>(json);
                if (envelope?.snapshot == null)
                    return;

                Store.Replace(envelope.snapshot, envelope.privateState);
                return;
            }

            if (type == "GAME_EVENT")
            {
                var envelope = JsonUtility.FromJson<GameEventEnvelopeDto>(json);
                if (envelope == null)
                    return;

                var expected = Store.Version + 1;
                if (Store.Version > 0 && envelope.version != expected)
                {
                    RequestResync();
                }
            }
        }

        public void RequestResync()
        {
            ResyncRequested?.Invoke();

            if (_transport == null || !_transport.IsConnected)
                return;

            var message =
                "{\"type\":\"SYNC_REQUEST\",\"roomCode\":\"" +
                Escape(roomCode) +
                "\",\"playerId\":\"" +
                Escape(playerId) +
                "\"}";

            _transport.Send(message);
        }

        private static string ExtractType(string json)
        {
            const string marker = "\"type\"";
            var markerIndex = json.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
                return string.Empty;

            var colonIndex = json.IndexOf(':', markerIndex + marker.Length);
            if (colonIndex < 0)
                return string.Empty;

            var quoteStart = json.IndexOf('"', colonIndex + 1);
            if (quoteStart < 0)
                return string.Empty;

            var quoteEnd = json.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0)
                return string.Empty;

            return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
        }

        private static string Escape(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
    }
}
