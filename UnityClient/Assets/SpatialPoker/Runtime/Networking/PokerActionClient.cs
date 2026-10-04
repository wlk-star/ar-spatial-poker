using System;
using UnityEngine;
using SpatialPoker.Poker;

namespace SpatialPoker.Networking
{
    public sealed class PokerActionClient : MonoBehaviour, IPokerIntentSink
    {
        [SerializeField] private string roomCode;
        [SerializeField] private string playerId;
        [SerializeField] private GameStateSynchronizer synchronizer;

        private IGameTransport _transport;

        public void Bind(IGameTransport transport)
        {
            _transport = transport;
            synchronizer?.Bind(transport);
        }

        public void Submit(in PokerIntent intent)
        {
            if (_transport == null ||
                !_transport.IsConnected ||
                synchronizer?.Store.Public == null)
            {
                return;
            }

            var snapshot = synchronizer.Store.Public;
            var action = intent.Type switch
            {
                PokerIntentType.Fold => "FOLD",
                PokerIntentType.Check => "CHECK",
                PokerIntentType.Call => "CALL",
                PokerIntentType.Bet => "BET",
                PokerIntentType.Raise => "RAISE",
                PokerIntentType.AllIn => "ALL_IN",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(action))
                return;

            var clientActionId = Guid.NewGuid().ToString("N");

            var json =
                "{\"type\":\"PLAYER_ACTION\"" +
                ",\"roomCode\":\"" + Escape(roomCode) + "\"" +
                ",\"handId\":\"" + Escape(snapshot.handId) + "\"" +
                ",\"playerId\":\"" + Escape(playerId) + "\"" +
                ",\"clientActionId\":\"" + clientActionId + "\"" +
                ",\"expectedVersion\":" + snapshot.version +
                ",\"action\":\"" + action + "\"" +
                ",\"amount\":" + intent.Amount +
                "}";

            _transport.Send(json);
        }

        private static string Escape(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
    }
}
