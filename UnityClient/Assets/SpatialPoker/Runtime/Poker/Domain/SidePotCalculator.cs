using System.Collections.Generic;
using System.Linq;

namespace SpatialPoker.Poker.Domain
{
    public sealed class PotSlice
    {
        public int Amount;
        public List<string> EligiblePlayerIds = new();
    }

    public static class SidePotCalculator
    {
        public static List<PotSlice> Build(IReadOnlyList<PokerPlayerState> players)
        {
            var result = new List<PotSlice>();
            var levels = players
                .Where(p => p.TotalContribution > 0)
                .Select(p => p.TotalContribution)
                .Distinct()
                .OrderBy(v => v)
                .ToArray();

            var previous = 0;

            foreach (var level in levels)
            {
                var contributors = players
                    .Where(p => p.TotalContribution >= level)
                    .ToList();

                var amount = (level - previous) * contributors.Count;
                if (amount <= 0)
                    continue;

                result.Add(new PotSlice
                {
                    Amount = amount,
                    EligiblePlayerIds = contributors
                        .Where(p => p.State != PlayerHandState.Folded &&
                                    p.State != PlayerHandState.SittingOut)
                        .Select(p => p.PlayerId)
                        .ToList()
                });

                previous = level;
            }

            return result;
        }
    }
}
