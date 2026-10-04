using System;

namespace SpatialPoker.Networking.Protocol
{
    [Serializable]
    public sealed class PublicPlayerSnapshotDto
    {
        public string playerId;
        public string displayName;
        public int seat;
        public int stack;
        public int streetContribution;
        public int totalContribution;
        public string state;
        public bool connected;
        public int holeCardCount;
    }

    [Serializable]
    public sealed class PublicGameSnapshotDto
    {
        public string roomCode;
        public int version;
        public string handId;
        public string street;
        public int dealerSeat;
        public int smallBlindSeat;
        public int bigBlindSeat;
        public int currentActionSeat;
        public int currentBet;
        public int minimumRaiseIncrement;
        public int pot;
        public string[] board;
        public PublicPlayerSnapshotDto[] players;
    }

    [Serializable]
    public sealed class LegalActionsDto
    {
        public string[] actions;
        public int callAmount;
        public int minRaiseTo;
        public int maxRaiseTo;
    }

    [Serializable]
    public sealed class PrivateGameStateDto
    {
        public string playerId;
        public string[] holeCards;
        public string reconnectToken;
        public LegalActionsDto legalActions;
    }

    [Serializable]
    public sealed class GameSnapshotEnvelopeDto
    {
        public string type;
        public PublicGameSnapshotDto snapshot;
        public PrivateGameStateDto privateState;
    }

    [Serializable]
    public sealed class GameEventEnvelopeDto
    {
        public string type;
        public int version;
        public string @event;
    }
}
