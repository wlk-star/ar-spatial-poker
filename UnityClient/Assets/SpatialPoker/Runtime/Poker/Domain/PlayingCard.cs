using System;

namespace SpatialPoker.Poker.Domain
{
    public enum Suit { Clubs, Diamonds, Hearts, Spades }
    public enum Rank
    {
        Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
        Jack, Queen, King, Ace
    }

    public readonly struct PlayingCard : IEquatable<PlayingCard>
    {
        public readonly Rank Rank;
        public readonly Suit Suit;

        public PlayingCard(Rank rank, Suit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        public bool Equals(PlayingCard other) =>
            Rank == other.Rank && Suit == other.Suit;

        public override bool Equals(object obj) =>
            obj is PlayingCard other && Equals(other);

        public override int GetHashCode() =>
            ((int)Rank * 397) ^ (int)Suit;

        public override string ToString() => $"{Rank}-{Suit}";
    }
}
