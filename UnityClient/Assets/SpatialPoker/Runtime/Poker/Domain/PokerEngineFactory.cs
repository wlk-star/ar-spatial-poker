using System.Collections.Generic;

namespace SpatialPoker.Poker.Domain
{
    public static class PokerEngineFactory
    {
        public static PokerEngine CreateDemoHeadsUp()
        {
            var state = new PokerTableState
            {
                HandId = "demo-hand-1",
                Street = PokerStreet.Preflop,
                DealerSeat = 0,
                SmallBlindSeat = 0,
                BigBlindSeat = 1,
                SmallBlind = 10,
                BigBlind = 20,
                CurrentActionSeat = 0,
                CurrentBet = 20,
                MinimumRaiseIncrement = 20,
                Pot = 30,
                Players = new List<PokerPlayerState>
                {
                    new()
                    {
                        PlayerId = "local-player",
                        Seat = 0,
                        Stack = 1990,
                        StreetContribution = 10,
                        TotalContribution = 10,
                        HasActedThisRound = false,
                        State = PlayerHandState.Active
                    },
                    new()
                    {
                        PlayerId = "opponent-player",
                        Seat = 1,
                        Stack = 1980,
                        StreetContribution = 20,
                        TotalContribution = 20,
                        HasActedThisRound = false,
                        State = PlayerHandState.Active
                    }
                }
            };

            return new PokerEngine(state, new Deck(42));
        }

        public static PokerEngine CreateFreshDemoTable()
        {
            var state = new PokerTableState
            {
                DealerSeat = 1,
                SmallBlind = 10,
                BigBlind = 20,
                Players = new List<PokerPlayerState>
                {
                    new() { PlayerId = "p0", Seat = 0, Stack = 2000, State = PlayerHandState.Active },
                    new() { PlayerId = "p1", Seat = 1, Stack = 2000, State = PlayerHandState.Active },
                    new() { PlayerId = "p2", Seat = 2, Stack = 2000, State = PlayerHandState.Active }
                }
            };

            return new PokerEngine(state, new Deck(42));
        }
    }
}
