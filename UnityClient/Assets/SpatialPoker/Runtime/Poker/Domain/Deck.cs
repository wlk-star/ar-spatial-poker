using System;
using System.Collections.Generic;

namespace SpatialPoker.Poker.Domain
{
    public sealed class Deck
    {
        private readonly List<PlayingCard> _cards = new();
        private int _next;

        public int Remaining => _cards.Count - _next;

        public Deck(int? seed = null)
        {
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                _cards.Add(new PlayingCard(rank, suit));

            Shuffle(seed);
        }

        public void Shuffle(int? seed = null)
        {
            var random = seed.HasValue ? new Random(seed.Value) : new Random();

            for (var i = _cards.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }

            _next = 0;
        }

        public PlayingCard Draw()
        {
            if (_next >= _cards.Count)
                throw new InvalidOperationException("Deck is empty.");

            return _cards[_next++];
        }

        public void Burn()
        {
            Draw();
        }
    }
}
