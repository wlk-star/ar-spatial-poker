using System;
using System.Collections.Generic;
using System.Linq;

namespace SpatialPoker.Poker.Domain
{
    public enum PokerActionError
    {
        None,
        UnknownPlayer,
        NotPlayersTurn,
        PlayerNotActive,
        IllegalAction,
        InvalidAmount,
        RaiseBelowMinimum
    }

    public readonly struct PokerActionResult
    {
        public readonly bool Accepted;
        public readonly PokerActionError Error;

        public PokerActionResult(bool accepted, PokerActionError error = PokerActionError.None)
        {
            Accepted = accepted;
            Error = error;
        }
    }

    public sealed class PokerEngine
    {
        private readonly Deck _deck;
        private readonly Dictionary<string, List<PlayingCard>> _holeCards = new();

        public PokerTableState State { get; }
        public IReadOnlyDictionary<string, List<PlayingCard>> HoleCards => _holeCards;

        public PokerEngine(PokerTableState state, Deck deck = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            _deck = deck ?? new Deck();
        }

        public void StartNewHand(string handId)
        {
            var seated = State.Players
                .Where(p => p.Stack > 0 && p.State != PlayerHandState.SittingOut)
                .OrderBy(p => p.Seat)
                .ToList();

            if (seated.Count < 2)
                throw new InvalidOperationException("At least two active players are required.");

            State.HandId = handId;
            State.Board.Clear();
            State.Pot = 0;
            State.CurrentBet = 0;
            State.MinimumRaiseIncrement = State.BigBlind;
            State.Street = PokerStreet.Preflop;

            foreach (var player in State.Players)
            {
                player.StreetContribution = 0;
                player.TotalContribution = 0;
                player.HasActedThisRound = false;
                player.State = player.Stack > 0
                    ? PlayerHandState.Active
                    : PlayerHandState.SittingOut;
            }

            _holeCards.Clear();
            _deck.Shuffle();

            State.DealerSeat = FindNextOccupiedSeat(State.DealerSeat, seated, includeCurrent: false);

            if (seated.Count == 2)
            {
                State.SmallBlindSeat = State.DealerSeat;
                State.BigBlindSeat = FindNextOccupiedSeat(State.DealerSeat, seated, false);
            }
            else
            {
                State.SmallBlindSeat = FindNextOccupiedSeat(State.DealerSeat, seated, false);
                State.BigBlindSeat = FindNextOccupiedSeat(State.SmallBlindSeat, seated, false);
            }

            PostBlind(State.SmallBlindSeat, State.SmallBlind);
            PostBlind(State.BigBlindSeat, State.BigBlind);
            State.CurrentBet = State.Players.First(p => p.Seat == State.BigBlindSeat).StreetContribution;

            DealHoleCards(seated);

            State.CurrentActionSeat = seated.Count == 2
                ? State.SmallBlindSeat
                : FindNextOccupiedSeat(State.BigBlindSeat, seated, false);
        }

        public PokerActionResult Apply(string playerId, in PokerIntent intent)
        {
            var player = State.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null)
                return new PokerActionResult(false, PokerActionError.UnknownPlayer);

            if (player.Seat != State.CurrentActionSeat)
                return new PokerActionResult(false, PokerActionError.NotPlayersTurn);

            if (player.State != PlayerHandState.Active)
                return new PokerActionResult(false, PokerActionError.PlayerNotActive);

            switch (intent.Type)
            {
                case PokerIntentType.Fold:
                    player.State = PlayerHandState.Folded;
                    player.HasActedThisRound = true;
                    ProgressAfterAction();
                    return new PokerActionResult(true);

                case PokerIntentType.Check:
                    if (player.StreetContribution != State.CurrentBet)
                        return new PokerActionResult(false, PokerActionError.IllegalAction);

                    player.HasActedThisRound = true;
                    ProgressAfterAction();
                    return new PokerActionResult(true);

                case PokerIntentType.Call:
                {
                    var needed = State.CurrentBet - player.StreetContribution;
                    if (needed <= 0)
                        return new PokerActionResult(false, PokerActionError.IllegalAction);

                    CommitChips(player, Math.Min(needed, player.Stack));
                    player.HasActedThisRound = true;
                    ProgressAfterAction();
                    return new PokerActionResult(true);
                }

                case PokerIntentType.Bet:
                case PokerIntentType.Raise:
                {
                    var raiseTo = intent.Amount;
                    if (raiseTo <= State.CurrentBet)
                        return new PokerActionResult(false, PokerActionError.InvalidAmount);

                    var increment = raiseTo - State.CurrentBet;
                    var allInTarget = player.StreetContribution + player.Stack;
                    var isAllInRaise = raiseTo == allInTarget;

                    if (increment < State.MinimumRaiseIncrement && !isAllInRaise)
                        return new PokerActionResult(false, PokerActionError.RaiseBelowMinimum);

                    var delta = raiseTo - player.StreetContribution;
                    if (delta <= 0 || delta > player.Stack)
                        return new PokerActionResult(false, PokerActionError.InvalidAmount);

                    CommitChips(player, delta);

                    if (increment >= State.MinimumRaiseIncrement)
                    {
                        State.MinimumRaiseIncrement = increment;
                        ResetActionFlagsExcept(player);
                    }

                    State.CurrentBet = Math.Max(State.CurrentBet, player.StreetContribution);
                    player.HasActedThisRound = true;
                    ProgressAfterAction();
                    return new PokerActionResult(true);
                }

                case PokerIntentType.AllIn:
                {
                    if (player.Stack <= 0)
                        return new PokerActionResult(false, PokerActionError.InvalidAmount);

                    var oldCurrentBet = State.CurrentBet;
                    var oldIncrement = State.MinimumRaiseIncrement;
                    CommitChips(player, player.Stack);
                    player.HasActedThisRound = true;

                    if (player.StreetContribution > oldCurrentBet)
                    {
                        var increment = player.StreetContribution - oldCurrentBet;
                        State.CurrentBet = player.StreetContribution;

                        if (increment >= oldIncrement)
                        {
                            State.MinimumRaiseIncrement = increment;
                            ResetActionFlagsExcept(player);
                            player.HasActedThisRound = true;
                        }
                    }

                    ProgressAfterAction();
                    return new PokerActionResult(true);
                }

                default:
                    return new PokerActionResult(false, PokerActionError.IllegalAction);
            }
        }

        public List<Payout> ResolveShowdown()
        {
            var publicHoleCards = _holeCards.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<PlayingCard>)kvp.Value);

            return ShowdownResolver.Resolve(State, publicHoleCards);
        }

        private void DealHoleCards(IReadOnlyList<PokerPlayerState> seated)
        {
            foreach (var player in seated)
                _holeCards[player.PlayerId] = new List<PlayingCard>(2);

            var firstSeat = FindNextOccupiedSeat(State.DealerSeat, seated, false);

            for (var round = 0; round < 2; round++)
            {
                var seat = firstSeat;
                for (var i = 0; i < seated.Count; i++)
                {
                    var player = seated.First(p => p.Seat == seat);
                    _holeCards[player.PlayerId].Add(_deck.Draw());
                    seat = FindNextOccupiedSeat(seat, seated, false);
                }
            }
        }

        private void PostBlind(int seat, int blind)
        {
            var player = State.Players.First(p => p.Seat == seat);
            CommitChips(player, Math.Min(blind, player.Stack));

            if (player.Stack == 0)
                player.State = PlayerHandState.AllIn;
        }

        private void CommitChips(PokerPlayerState player, int amount)
        {
            amount = Math.Max(0, Math.Min(amount, player.Stack));
            player.Stack -= amount;
            player.StreetContribution += amount;
            player.TotalContribution += amount;
            State.Pot += amount;

            if (player.Stack == 0)
                player.State = PlayerHandState.AllIn;
        }

        private void ProgressAfterAction()
        {
            var contenders = State.Players
                .Where(p => p.State != PlayerHandState.Folded &&
                            p.State != PlayerHandState.SittingOut)
                .ToList();

            if (contenders.Count <= 1)
            {
                State.CurrentActionSeat = -1;
                State.Street = PokerStreet.Settlement;
                return;
            }

            if (IsBettingRoundComplete())
            {
                AdvanceStreet();
                return;
            }

            AdvanceTurn();
        }

        private bool IsBettingRoundComplete()
        {
            var active = State.Players
                .Where(p => p.State == PlayerHandState.Active && p.Stack > 0)
                .ToList();

            if (active.Count == 0)
                return true;

            foreach (var player in active)
            {
                if (!player.HasActedThisRound)
                    return false;

                if (player.StreetContribution != State.CurrentBet)
                    return false;
            }

            return true;
        }

        private void AdvanceStreet()
        {
            switch (State.Street)
            {
                case PokerStreet.Preflop:
                    _deck.Burn();
                    State.Board.Add(_deck.Draw());
                    State.Board.Add(_deck.Draw());
                    State.Board.Add(_deck.Draw());
                    State.Street = PokerStreet.Flop;
                    break;

                case PokerStreet.Flop:
                    _deck.Burn();
                    State.Board.Add(_deck.Draw());
                    State.Street = PokerStreet.Turn;
                    break;

                case PokerStreet.Turn:
                    _deck.Burn();
                    State.Board.Add(_deck.Draw());
                    State.Street = PokerStreet.River;
                    break;

                case PokerStreet.River:
                    State.Street = PokerStreet.Showdown;
                    State.CurrentActionSeat = -1;
                    return;
            }

            State.CurrentBet = 0;
            State.MinimumRaiseIncrement = State.BigBlind;

            foreach (var player in State.Players)
            {
                player.StreetContribution = 0;
                player.HasActedThisRound = false;
            }

            var actionable = State.Players
                .Where(p => p.State == PlayerHandState.Active && p.Stack > 0)
                .OrderBy(p => p.Seat)
                .ToList();

            if (actionable.Count <= 1)
            {
                RunBoardToShowdownIfNeeded();
                return;
            }

            State.CurrentActionSeat = FindNextOccupiedSeat(
                State.DealerSeat,
                actionable,
                false);
        }

        private void RunBoardToShowdownIfNeeded()
        {
            while (State.Street != PokerStreet.Showdown)
            {
                if (State.Street == PokerStreet.Preflop)
                {
                    _deck.Burn();
                    State.Board.Add(_deck.Draw());
                    State.Board.Add(_deck.Draw());
                    State.Board.Add(_deck.Draw());
                    State.Street = PokerStreet.Flop;
                }
                else if (State.Street == PokerStreet.Flop)
                {
                    _deck.Burn();
                    State.Board.Add(_deck.Draw());
                    State.Street = PokerStreet.Turn;
                }
                else if (State.Street == PokerStreet.Turn)
                {
                    _deck.Burn();
                    State.Board.Add(_deck.Draw());
                    State.Street = PokerStreet.River;
                }
                else if (State.Street == PokerStreet.River)
                {
                    State.Street = PokerStreet.Showdown;
                }
                else
                {
                    break;
                }
            }

            State.CurrentActionSeat = -1;
        }

        private void AdvanceTurn()
        {
            var actionable = State.Players
                .Where(p => p.State == PlayerHandState.Active && p.Stack > 0)
                .OrderBy(p => p.Seat)
                .ToList();

            if (actionable.Count == 0)
            {
                RunBoardToShowdownIfNeeded();
                return;
            }

            State.CurrentActionSeat = FindNextOccupiedSeat(
                State.CurrentActionSeat,
                actionable,
                false);
        }

        private void ResetActionFlagsExcept(PokerPlayerState actor)
        {
            foreach (var player in State.Players)
            {
                if (player.State == PlayerHandState.Active)
                    player.HasActedThisRound = false;
            }

            actor.HasActedThisRound = true;
        }

        private static int FindNextOccupiedSeat(
            int fromSeat,
            IReadOnlyList<PokerPlayerState> players,
            bool includeCurrent)
        {
            var ordered = players.OrderBy(p => p.Seat).ToList();

            if (includeCurrent)
            {
                var current = ordered.FirstOrDefault(p => p.Seat == fromSeat);
                if (current != null)
                    return current.Seat;
            }

            var next = ordered.FirstOrDefault(p => p.Seat > fromSeat);
            return next?.Seat ?? ordered[0].Seat;
        }
    }
}
