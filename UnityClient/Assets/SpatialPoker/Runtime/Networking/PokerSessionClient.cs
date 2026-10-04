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

        private void Awake()
        {
            if (transport != null)
            {
                synchronizer?.Bind(transport);
                actionClient?.Bind(transport);
            }

            synchronizer?.ConfigureIdentity(RoomCode, playerId);
        }

        public void Connect() => transport?.Connect();

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

        public void JoinRoom(string roomCode)
        {
            if (transport == null || !transport.IsConnected)
                return;

            RoomCode = roomCode;
            synchronizer?.ConfigureIdentity(RoomCode, playerId);

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

            transport.Send(
                "{\"type\":\"START_HAND\",\"roomCode\":\"" +
                Escape(roomCode) +
                "\"}");
        }

        public void Sync()
        {
            RoomCode = synchronizer?.Store.Public?.roomCode ?? RoomCode;
            synchronizer?.ConfigureIdentity(RoomCode, playerId);
            synchronizer?.RequestResync();
        }

        private static string Escape(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
    }
}
