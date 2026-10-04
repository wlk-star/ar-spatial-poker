using System.Collections.Generic;

namespace SpatialPoker.Poker.Domain
{
    public enum PokerStreet
    {
        Waiting,
        Preflop,
        Flop,
        Turn,
        River,
        Showdown,
        Settlement,
        HandResult
    }

    public enum PlayerHandState
    {
        Active,
        Folded,
        AllIn,
        SittingOut
    }

    public sealed class PokerPlayerState
    {
        public string PlayerId;
        public int Seat;
        public int Stack;
        public int StreetContribution;
        public int TotalContribution;
        public bool HasActedThisRound;
        public PlayerHandState State;
    }

    public sealed class PokerTableState
    {
        public string HandId;
        public PokerStreet Street;
        public int DealerSeat;
        public int SmallBlindSeat;
        public int BigBlindSeat;
        public int SmallBlind;
        public int BigBlind;
        public int CurrentActionSeat;
        public int CurrentBet;
        public int MinimumRaiseIncrement;
        public int Pot;
        public List<PlayingCard> Board = new();
        public List<PokerPlayerState> Players = new();
    }
}
