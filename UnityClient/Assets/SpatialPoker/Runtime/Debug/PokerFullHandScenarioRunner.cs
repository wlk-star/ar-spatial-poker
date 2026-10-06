using System.Collections.Generic;
using UnityEngine;
using SpatialPoker.Poker;
using SpatialPoker.Poker.Domain;

namespace SpatialPoker.Debugging
{
    public sealed class PokerFullHandScenarioRunner : MonoBehaviour
    {
        [ContextMenu("Run Full Poker Domain Scenarios")]
        public void Run()
        {
            ValidateFreshHandLifecycle();
            ValidateHandEvaluator();
            ValidateSidePots();
            ValidateUncontestedSettlement();
            Debug.Log("[PokerFullHandScenarioRunner] All domain scenarios passed.", this);
        }

        private static void ValidateFreshHandLifecycle()
        {
            var engine = PokerEngineFactory.CreateFreshDemoTable();
            engine.StartNewHand("hand-001");

            Debug.Assert(engine.State.Street == PokerStreet.Preflop);
            Debug.Assert(engine.State.Board.Count == 0);
            Debug.Assert(engine.State.Pot == 30);
            Debug.Assert(engine.HoleCards.Count == 3);

            foreach (var cards in engine.HoleCards.Values)
                Debug.Assert(cards.Count == 2);
        }

        private static void ValidateHandEvaluator()
        {
            var straightFlush = HandEvaluator.EvaluateBest(new[]
            {
                new PlayingCard(Rank.Ace, Suit.Spades),
                new PlayingCard(Rank.King, Suit.Spades),
                new PlayingCard(Rank.Queen, Suit.Spades),
                new PlayingCard(Rank.Jack, Suit.Spades),
                new PlayingCard(Rank.Ten, Suit.Spades),
                new PlayingCard(Rank.Two, Suit.Clubs),
                new PlayingCard(Rank.Three, Suit.Diamonds)
            });

            var quads = HandEvaluator.EvaluateBest(new[]
            {
                new PlayingCard(Rank.Ace, Suit.Spades),
                new PlayingCard(Rank.Ace, Suit.Hearts),
                new PlayingCard(Rank.Ace, Suit.Diamonds),
                new PlayingCard(Rank.Ace, Suit.Clubs),
                new PlayingCard(Rank.King, Suit.Spades)
            });

            Debug.Assert(straightFlush.Category == HandCategory.StraightFlush);
            Debug.Assert(quads.Category == HandCategory.FourOfAKind);
            Debug.Assert(straightFlush.Score > quads.Score);
        }

        private static void ValidateSidePots()
        {
            var players = new List<PokerPlayerState>
            {
                new() { PlayerId = "a", TotalContribution = 100, State = PlayerHandState.AllIn },
                new() { PlayerId = "b", TotalContribution = 300, State = PlayerHandState.AllIn },
                new() { PlayerId = "c", TotalContribution = 300, State = PlayerHandState.Active }
            };

            var pots = SidePotCalculator.Build(players);

            Debug.Assert(pots.Count == 2);
            Debug.Assert(pots[0].Amount == 300);
            Debug.Assert(pots[1].Amount == 400);
            Debug.Assert(pots[0].EligiblePlayerIds.Count == 3);
            Debug.Assert(pots[1].EligiblePlayerIds.Count == 2);
        }

        private static void ValidateUncontestedSettlement()
        {
            var engine = PokerEngineFactory.CreateDemoHeadsUp();

            var fold = engine.Apply("local-player", PokerIntent.Fold());
            Debug.Assert(fold.Accepted);
            Debug.Assert(engine.State.Street == PokerStreet.Settlement);

            var opponentBefore = engine.State.Players[1].Stack;
            var pot = engine.State.Pot;
            var payouts = engine.SettleHand();

            Debug.Assert(payouts.Count == 1);
            Debug.Assert(payouts[0].Amount == pot);
            Debug.Assert(engine.State.Players[1].Stack == opponentBefore + pot);
            Debug.Assert(engine.State.Pot == 0);
            Debug.Assert(engine.State.Street == PokerStreet.HandResult);
        }
    }
}
