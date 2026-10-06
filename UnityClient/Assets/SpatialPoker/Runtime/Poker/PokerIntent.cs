namespace SpatialPoker.Poker
{
    public enum PokerIntentType
    {
        Bet,
        Raise,
        Call,
        Check,
        Fold,
        AllIn
    }

    public readonly struct PokerIntent
    {
        public readonly PokerIntentType Type;
        public readonly int Amount;

        public PokerIntent(PokerIntentType type, int amount = 0)
        {
            Type = type;
            Amount = amount;
        }

        public static PokerIntent Bet(int amount) =>
            new(PokerIntentType.Bet, amount);

        public static PokerIntent Raise(int amount) =>
            new(PokerIntentType.Raise, amount);

        public static PokerIntent Call() =>
            new(PokerIntentType.Call);

        public static PokerIntent Check() =>
            new(PokerIntentType.Check);

        public static PokerIntent Fold() =>
            new(PokerIntentType.Fold);

        public static PokerIntent AllIn(int amount) =>
            new(PokerIntentType.AllIn, amount);
    }

    public interface IPokerIntentSink
    {
        void Submit(in PokerIntent intent);
    }
}
