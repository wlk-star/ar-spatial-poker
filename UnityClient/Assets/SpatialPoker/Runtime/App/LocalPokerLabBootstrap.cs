using System;
using UnityEngine;
using SpatialPoker.Networking;

namespace SpatialPoker.App
{
    /// <summary>
    /// Editor lab bootstrap: connect -&gt; create a LOCAL_BOT room -&gt; start
    /// the hand once two seats are present. Idempotent startup, reports status
    /// through <see cref="StatusChanged"/>. Encodes no poker rules and never
    /// mutates authoritative snapshots.
    /// </summary>
    public sealed class LocalPokerLabBootstrap : MonoBehaviour
    {
        [SerializeField] private PokerSessionClient session;
        [SerializeField] private GameStateSynchronizer synchronizer;

        public event Action<string> StatusChanged;

        private bool _roomRequested;
        private bool _handStarted;

        private void OnEnable()
        {
            if (session != null)
            {
                session.Connected += OnConnected;
                session.Disconnected += OnDisconnected;
                session.RoomCreated += OnRoomCreated;
            }

            if (synchronizer != null)
                synchronizer.Store.Changed += OnStoreChanged;
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.Connected -= OnConnected;
                session.Disconnected -= OnDisconnected;
                session.RoomCreated -= OnRoomCreated;
            }

            if (synchronizer != null)
                synchronizer.Store.Changed -= OnStoreChanged;

            _roomRequested = false;
            _handStarted = false;
        }

        private void Start()
        {
            Report("Connecting to local server…");
            session?.Connect();
        }

        private void OnConnected()
        {
            if (_roomRequested)
                return;

            _roomRequested = true;
            Report("Requesting LOCAL_BOT room…");
            session?.CreateLocalBotRoom();
        }

        private void OnRoomCreated(string roomCode)
        {
            Report($"Room {roomCode} created. Waiting for Bot seat…");
            TryStartHand();
        }

        private void OnDisconnected()
        {
            _roomRequested = false;
            _handStarted = false;
            Report("Disconnected from server.");
        }

        private void OnStoreChanged() => TryStartHand();

        private void TryStartHand()
        {
            if (_handStarted || session == null || synchronizer == null)
                return;

            var snapshot = synchronizer.Store.Public;
            if (snapshot?.players == null ||
                snapshot.players.Length < 2 ||
                !string.IsNullOrEmpty(snapshot.handId))
            {
                return;
            }

            _handStarted = true;
            Report($"Starting hand in room {snapshot.roomCode}…");
            session.StartHand();
        }

        private void Report(string message)
        {
            Debug.Log($"[LocalPokerLab] {message}", this);
            StatusChanged?.Invoke(message);
        }
    }
}
