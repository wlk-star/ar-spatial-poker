using System;
using UnityEngine;

namespace SpatialPoker.Networking
{
    public sealed class PokerSessionClient : MonoBehaviour
    {
        [SerializeField] private ClientWebSocketTransport transport;
        [SerializeField] private GameStateSynchronizer synchronizer;
        [SerializeField] private PokerActionClient actionClient;
        [SerializeField] private string playerId = "local-player";
        [SerializeField] private string displayName = "Player";

        public string RoomCode { get; private set; }
        public string ReconnectToken =>
            synchronizer?.Store.Private?.reconnectToken;

        public bool IsConnected => transport != null && transport.IsConnected;

        public event Action Connected;
        public event Action Disconnected;
        public event Action<string> RoomCreated;

        private bool _roomCreatedFired;

        private void Awake()
        {
            if (transport != null)
            {
                transport.Connected += OnTransportConnected;
                transport.Disconnected += OnTransportDisconnected;

                synchronizer?.Bind(transport);
                actionClient?.Bind(transport);
            }

            synchronizer?.ConfigureIdentity(RoomCode, playerId);
            actionClient?.ConfigureIdentity(RoomCode, playerId);

            if (synchronizer != null)
                synchronizer.Store.Changed += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (transport != null)
            {
                transport.Connected -= OnTransportConnected;
                transport.Disconnected -= OnTransportDisconnected;
            }

            if (synchronizer != null)
                synchronizer.Store.Changed -= OnStateChanged;
        }

        private void OnStateChanged()
        {
            var serverRoomCode = synchronizer?.Store.Public?.roomCode;
            if (!string.IsNullOrEmpty(serverRoomCode))
                RoomCode = serverRoomCode;

            synchronizer?.ConfigureIdentity(RoomCode, playerId);
            actionClient?.ConfigureIdentity(RoomCode, playerId);

            if (!_roomCreatedFired && !string.IsNullOrEmpty(RoomCode))
            {
                _roomCreatedFired = true;
                RoomCreated?.Invoke(RoomCode);
            }
        }

        private void OnTransportConnected() => Connected?.Invoke();

        private void OnTransportDisconnected() => Disconnected?.Invoke();

        public void Connect()
        {
            _roomCreatedFired = false;
            transport?.Connect();
        }

        public void CreateRoom()
        {
            if (transport == null || !transport.IsConnected)
                return;

            transport.Send(
                "{\"type\":\"CREATE_ROOM\",\"playerId\":\"" +
                Escape(playerId) +
                "\",\"displayName\":\"" +
                Escape(displayName) +
                "\"}");
        }

        public void CreateLocalBotRoom()
        {
            if (transport == null || !transport.IsConnected)
                return;

            transport.Send(
                "{\"type\":\"CREATE_ROOM\",\"playerId\":\"" +
                Escape(playerId) +
                "\",\"displayName\":\"" +
                Escape(displayName) +
                "\",\"opponentMode\":\"LOCAL_BOT\"}");
        }

        public void JoinRoom(string roomCode)
        {
            if (transport == null || !transport.IsConnected)
                return;

            RoomCode = roomCode;
            synchronizer?.ConfigureIdentity(RoomCode, playerId);
            actionClient?.ConfigureIdentity(RoomCode, playerId);

            var tokenPart = string.IsNullOrEmpty(ReconnectToken)
                ? string.Empty
                : ",\"reconnectToken\":\"" + Escape(ReconnectToken) + "\"";

            transport.Send(
                "{\"type\":\"JOIN_ROOM\",\"roomCode\":\"" +
                Escape(roomCode) +
                "\",\"playerId\":\"" +
                Escape(playerId) +
                "\",\"displayName\":\"" +
                Escape(displayName) +
                "\"" +
                tokenPart +
                "}");
        }

        public void StartHand()
        {
            var roomCode = synchronizer?.Store.Public?.roomCode ?? RoomCode;
            if (transport == null ||
                !transport.IsConnected ||
                string.IsNullOrEmpty(roomCode))
            {
                return;
            }

            RoomCode = roomCode;
            synchronizer?.ConfigureIdentity(RoomCode, playerId);
            actionClient?.ConfigureIdentity(RoomCode, playerId);

            transport.Send(
                "{\"type\":\"START_HAND\",\"roomCode\":\"" +
                Escape(roomCode) +
                "\"}");
        }

        public void Sync()
        {
            RoomCode = synchronizer?.Store.Public?.roomCode ?? RoomCode;
            synchronizer?.ConfigureIdentity(RoomCode, playerId);
            actionClient?.ConfigureIdentity(RoomCode, playerId);
            synchronizer?.RequestResync();
        }

        private static string Escape(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
    }
}
