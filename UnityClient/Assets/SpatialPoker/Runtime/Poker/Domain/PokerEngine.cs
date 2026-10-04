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
                    AdvanceTurn();
                    return new PokerActionResult(true);

                case PokerIntentType.Check:
                    if (player.StreetContribution != State.CurrentBet)
                        return new PokerActionResult(false, PokerActionError.IllegalAction);

                    AdvanceTurn();
                    return new PokerActionResult(true);

                case PokerIntentType.Call:
                {
                    var needed = State.CurrentBet - player.StreetContribution;
                    if (needed <= 0)
                        return new PokerActionResult(false, PokerActionError.IllegalAction);

                    CommitChips(player, Math.Min(needed, player.Stack));
                    AdvanceTurn();
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
                    AdvanceTurn();
                    return new PokerActionResult(true);
                }

                case PokerIntentType.AllIn:
                {
                    if (player.Stack <= 0)
                        return new PokerActionResult(false, PokerActionError.InvalidAmount);

                    CommitChips(player, player.Stack);
                    player.State = PlayerHandState.AllIn;
                    State.CurrentBet = Math.Max(State.CurrentBet, player.StreetContribution);
                    AdvanceTurn();
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
