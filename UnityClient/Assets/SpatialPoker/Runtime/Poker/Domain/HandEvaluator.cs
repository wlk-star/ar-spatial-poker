using System;
using System.Collections.Generic;
using System.Linq;

namespace SpatialPoker.Poker.Domain
{
    public enum HandCategory
    {
        HighCard = 0,
        OnePair = 1,
        TwoPair = 2,
        ThreeOfAKind = 3,
        Straight = 4,
        Flush = 5,
        FullHouse = 6,
        FourOfAKind = 7,
        StraightFlush = 8
    }

    public readonly struct HandRank : IComparable<HandRank>
    {
        public readonly HandCategory Category;
        public readonly long Score;

        public HandRank(HandCategory category, long score)
        {
            Category = category;
            Score = score;
        }

        public int CompareTo(HandRank other) => Score.CompareTo(other.Score);
        public override string ToString() => Category.ToString();
    }

    public static class HandEvaluator
    {
        public static HandRank EvaluateBest(IReadOnlyList<PlayingCard> cards)
        {
            if (cards == null || cards.Count < 5 || cards.Count > 7)
                throw new ArgumentException("Evaluator requires 5 to 7 cards.");

            var best = new HandRank(HandCategory.HighCard, -1);

            for (var a = 0; a < cards.Count - 4; a++)
            for (var b = a + 1; b < cards.Count - 3; b++)
            for (var c = b + 1; c < cards.Count - 2; c++)
            for (var d = c + 1; d < cards.Count - 1; d++)
            for (var e = d + 1; e < cards.Count; e++)
            {
                var candidate = EvaluateFive(new[]
                {
                    cards[a], cards[b], cards[c], cards[d], cards[e]
                });

                if (candidate.CompareTo(best) > 0)
                    best = candidate;
            }

            return best;
        }

        private static HandRank EvaluateFive(IReadOnlyList<PlayingCard> cards)
        {
            var ranks = cards.Select(c => (int)c.Rank)
                .OrderByDescending(v => v)
                .ToArray();

            var groups = ranks.GroupBy(v => v)
                .Select(g => new { Rank = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenByDescending(g => g.Rank)
                .ToArray();

            var flush = cards.All(c => c.Suit == cards[0].Suit);
            var straightHigh = GetStraightHigh(ranks);

            if (flush && straightHigh > 0)
                return Build(HandCategory.StraightFlush, straightHigh);

            if (groups[0].Count == 4)
                return Build(HandCategory.FourOfAKind, groups[0].Rank, groups[1].Rank);

            if (groups[0].Count == 3 && groups[1].Count == 2)
                return Build(HandCategory.FullHouse, groups[0].Rank, groups[1].Rank);

            if (flush)
                return Build(HandCategory.Flush, ranks);

            if (straightHigh > 0)
                return Build(HandCategory.Straight, straightHigh);

            if (groups[0].Count == 3)
            {
                var kickers = groups.Where(g => g.Count == 1).Select(g => g.Rank).OrderByDescending(v => v);
                return Build(HandCategory.ThreeOfAKind, new[] { groups[0].Rank }.Concat(kickers).ToArray());
            }

            var pairs = groups.Where(g => g.Count == 2).OrderByDescending(g => g.Rank).ToArray();
            if (pairs.Length >= 2)
            {
                var kicker = groups.Where(g => g.Count == 1).Max(g => g.Rank);
                return Build(HandCategory.TwoPair, pairs[0].Rank, pairs[1].Rank, kicker);
            }

            if (pairs.Length == 1)
            {
                var kickers = groups.Where(g => g.Count == 1).Select(g => g.Rank).OrderByDescending(v => v);
                return Build(HandCategory.OnePair, new[] { pairs[0].Rank }.Concat(kickers).ToArray());
            }

            return Build(HandCategory.HighCard, ranks);
        }

        private static int GetStraightHigh(IEnumerable<int> ranks)
        {
            var distinct = ranks.Distinct().OrderByDescending(v => v).ToList();
            if (distinct.Contains(14))
                distinct.Add(1);

            var run = 1;
            for (var i = 1; i < distinct.Count; i++)
            {
                if (distinct[i - 1] - 1 == distinct[i])
                {
                    run++;
                    if (run >= 5)
                        return distinct[i - 4];
                }
                else
                {
                    run = 1;
                }
            }

            return 0;
        }

        private static HandRank Build(HandCategory category, params int[] tieBreakers)
        {
            long score = (long)category << 24;
            var shift = 20;

            foreach (var value in tieBreakers.Take(5))
            {
                score |= (long)value << shift;
                shift -= 4;
            }

            return new HandRank(category, score);
        }
    }
}
