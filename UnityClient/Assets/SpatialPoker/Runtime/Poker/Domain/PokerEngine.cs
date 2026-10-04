using System;
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
        InvalidAmount
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
        public PokerTableState State { get; }

        public PokerEngine(PokerTableState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
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
                    if (intent.Amount <= State.CurrentBet)
                        return new PokerActionResult(false, PokerActionError.InvalidAmount);

                    var delta = intent.Amount - player.StreetContribution;
                    if (delta <= 0 || delta > player.Stack)
                        return new PokerActionResult(false, PokerActionError.InvalidAmount);

                    CommitChips(player, delta);
                    State.CurrentBet = player.StreetContribution;

                    foreach (var other in State.Players)
                    {
                        if (other.State == PlayerHandState.Active)
                            other.HasActedThisRound = false;
                    }

                    player.HasActedThisRound = true;
                    ProgressAfterAction();
                    return new PokerActionResult(true);
                }

                case PokerIntentType.AllIn:
                {
                    if (player.Stack <= 0)
                        return new PokerActionResult(false, PokerActionError.InvalidAmount);

                    var oldCurrentBet = State.CurrentBet;
                    CommitChips(player, player.Stack);
                    player.HasActedThisRound = true;

                    if (player.StreetContribution > oldCurrentBet)
                    {
                        State.CurrentBet = player.StreetContribution;

                        foreach (var other in State.Players)
                        {
                            if (other.State == PlayerHandState.Active)
                                other.HasActedThisRound = false;
                        }

                        player.HasActedThisRound = true;
                    }

                    ProgressAfterAction();
                    return new PokerActionResult(true);
                }

                default:
                    return new PokerActionResult(false, PokerActionError.IllegalAction);
            }
        }

        private void CommitChips(PokerPlayerState player, int amount)
        {
            player.Stack -= amount;
            player.StreetContribution += amount;
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
            foreach (var player in State.Players)
            {
                if (player.State == PlayerHandState.Folded ||
                    player.State == PlayerHandState.SittingOut ||
                    player.State == PlayerHandState.AllIn)
                {
                    continue;
                }

                if (!player.HasActedThisRound)
                    return false;

                if (player.StreetContribution != State.CurrentBet)
                    return false;
            }

            return true;
        }

        private void AdvanceStreet()
        {
            State.Street = State.Street switch
            {
                PokerStreet.Preflop => PokerStreet.Flop,
                PokerStreet.Flop => PokerStreet.Turn,
                PokerStreet.Turn => PokerStreet.River,
                PokerStreet.River => PokerStreet.Showdown,
                _ => State.Street
            };

            State.CurrentBet = 0;

            foreach (var player in State.Players)
            {
                player.StreetContribution = 0;
                player.HasActedThisRound = false;
            }

            if (State.Street == PokerStreet.Showdown)
            {
                State.CurrentActionSeat = -1;
                return;
            }

            var next = State.Players
                .Where(p => p.State == PlayerHandState.Active && p.Stack > 0)
                .OrderBy(p => p.Seat)
                .FirstOrDefault();

            State.CurrentActionSeat = next?.Seat ?? -1;
        }

        private void AdvanceTurn()
        {
            var actionable = State.Players
                .Where(p => p.State == PlayerHandState.Active && p.Stack > 0)
                .OrderBy(p => p.Seat)
                .ToList();

            if (actionable.Count == 0)
            {
                State.CurrentActionSeat = -1;
                return;
            }

            var next = actionable.FirstOrDefault(p => p.Seat > State.CurrentActionSeat)
                       ?? actionable[0];

            State.CurrentActionSeat = next.Seat;
        }
    }
}
