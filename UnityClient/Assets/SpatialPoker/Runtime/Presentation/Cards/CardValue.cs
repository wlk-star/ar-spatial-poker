namespace SpatialPoker.Presentation.Cards
{
    public enum CardSuit { Clubs, Diamonds, Hearts, Spades }
    public enum CardRank
    {
        Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
        Jack, Queen, King, Ace
    }

    public readonly struct CardValue
    {
        public readonly CardRank Rank;
        public readonly CardSuit Suit;

        public CardValue(CardRank rank, CardSuit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        public override string ToString() => $"{Rank} of {Suit}";
    }
}
