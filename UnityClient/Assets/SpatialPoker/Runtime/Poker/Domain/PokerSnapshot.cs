using System.Collections.Generic;
using System.Linq;

namespace SpatialPoker.Poker.Domain
{
    public sealed class PokerPlayerSnapshot
    {
        public string PlayerId;
        public int Seat;
        public int Stack;
        public int StreetContribution;
        public int TotalContribution;
        public PlayerHandState State;
        public int HoleCardCount;
    }

    public sealed class PokerSnapshot
    {
        public string HandId;
        public PokerStreet Street;
        public int DealerSeat;
        public int SmallBlindSeat;
        public int BigBlindSeat;
        public int CurrentActionSeat;
        public int CurrentBet;
        public int MinimumRaiseIncrement;
        public int Pot;
        public List<PlayingCard> Board = new();
        public List<PokerPlayerSnapshot> Players = new();
        public List<PlayingCard> LocalHoleCards = new();
    }

    public static class PokerSnapshotFactory
    {
        public static PokerSnapshot CreateForPlayer(
            PokerEngine engine,
            string viewerPlayerId,
            bool revealShowdownCards = false)
        {
            var state = engine.State;
            var snapshot = new PokerSnapshot
            {
                HandId = state.HandId,
                Street = state.Street,
                DealerSeat = state.DealerSeat,
                SmallBlindSeat = state.SmallBlindSeat,
                BigBlindSeat = state.BigBlindSeat,
                CurrentActionSeat = state.CurrentActionSeat,
                CurrentBet = state.CurrentBet,
                MinimumRaiseIncrement = state.MinimumRaiseIncrement,
                Pot = state.Pot,
                Board = state.Board.ToList()
            };

            foreach (var player in state.Players)
            {
                engine.HoleCards.TryGetValue(player.PlayerId, out var holeCards);

                snapshot.Players.Add(new PokerPlayerSnapshot
                {
                    PlayerId = player.PlayerId,
                    Seat = player.Seat,
                    Stack = player.Stack,
                    StreetContribution = player.StreetContribution,
                    TotalContribution = player.TotalContribution,
                    State = player.State,
                    HoleCardCount = holeCards?.Count ?? 0
                });

                if (player.PlayerId == viewerPlayerId && holeCards != null)
                    snapshot.LocalHoleCards = holeCards.ToList();
            }

            // Showdown reveal is intentionally not embedded into opponent player
            // snapshots. A future explicit reveal event can carry only the
            // server-authorized cards for the relevant seats.
            _ = revealShowdownCards;

            return snapshot;
        }
    }
}
