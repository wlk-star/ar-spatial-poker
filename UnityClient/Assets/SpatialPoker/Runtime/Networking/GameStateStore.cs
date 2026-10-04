using System;
using SpatialPoker.Networking.Protocol;

namespace SpatialPoker.Networking
{
    public sealed class GameStateStore
    {
        public PublicGameSnapshotDto Public { get; private set; }
        public PrivateGameStateDto Private { get; private set; }
        public int Version => Public?.version ?? 0;

        public event Action Changed;

        public void Replace(
            PublicGameSnapshotDto publicSnapshot,
            PrivateGameStateDto privateState)
        {
            Public = publicSnapshot;
            Private = privateState;
            Changed?.Invoke();
        }
    }
}
