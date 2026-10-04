using System.Collections.Generic;
using System.Linq;

namespace SpatialPoker.Poker.Domain
{
    public sealed class Payout
    {
        public string PlayerId;
        public int Amount;
        public HandRank HandRank;
    }

    public static class ShowdownResolver
    {
        public static List<Payout> Resolve(
            PokerTableState state,
            IReadOnlyDictionary<string, IReadOnlyList<PlayingCard>> holeCards)
        {
            var payouts = new Dictionary<string, Payout>();
            var pots = SidePotCalculator.Build(state.Players);

            foreach (var pot in pots)
            {
                var ranked = pot.EligiblePlayerIds
                    .Where(holeCards.ContainsKey)
                    .Select(id => new
                    {
                        PlayerId = id,
                        Rank = HandEvaluator.EvaluateBest(
                            holeCards[id].Concat(state.Board).ToArray())
                    })
                    .OrderByDescending(x => x.Rank.Score)
                    .ToList();

                if (ranked.Count == 0)
                    continue;

                var best = ranked[0].Rank.Score;
                var winners = ranked.Where(x => x.Rank.Score == best).ToList();
                var share = pot.Amount / winners.Count;
                var remainder = pot.Amount % winners.Count;

                for (var i = 0; i < winners.Count; i++)
                {
                    var win = winners[i];
                    var amount = share + (i < remainder ? 1 : 0);

                    if (!payouts.TryGetValue(win.PlayerId, out var payout))
                    {
                        payout = new Payout
                        {
                            PlayerId = win.PlayerId,
                            HandRank = win.Rank
                        };
                        payouts.Add(win.PlayerId, payout);
                    }

                    payout.Amount += amount;
                }
            }

            return payouts.Values.ToList();
        }
    }
}
