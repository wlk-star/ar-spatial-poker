using UnityEngine;
using SpatialPoker.Poker;
using SpatialPoker.Poker.Domain;

namespace SpatialPoker.Debugging
{
    public sealed class PokerEngineScenarioRunner : MonoBehaviour
    {
        [ContextMenu("Run Poker Engine Scenarios")]
        public void Run()
        {
            RunCallCheckAdvancesToFlop();
            RunFoldEndsHeadsUpHand();
            RunRaiseRequiresResponse();
            Debug.Log("[PokerEngineScenarioRunner] All scenarios passed.", this);
        }

        private static void RunCallCheckAdvancesToFlop()
        {
            var engine = PokerEngineFactory.CreateDemoHeadsUp();

            var call = engine.Apply("local-player", PokerIntent.Call());
            Debug.Assert(call.Accepted, "Local preflop call should be accepted.");
            Debug.Assert(engine.State.CurrentActionSeat == 1, "Opponent should act after call.");

            var check = engine.Apply("opponent-player", PokerIntent.Check());
            Debug.Assert(check.Accepted, "Big blind check should be accepted.");
            Debug.Assert(engine.State.Street == PokerStreet.Flop, "Completed preflop should advance to flop.");
            Debug.Assert(engine.State.CurrentBet == 0, "Current bet resets on new street.");
            Debug.Assert(engine.State.CurrentActionSeat == 0, "Lowest active seat acts first in current demo postflop policy.");
        }

        private static void RunFoldEndsHeadsUpHand()
        {
            var engine = PokerEngineFactory.CreateDemoHeadsUp();

            var fold = engine.Apply("local-player", PokerIntent.Fold());
            Debug.Assert(fold.Accepted, "Fold should be accepted.");
            Debug.Assert(engine.State.Street == PokerStreet.Settlement, "Single remaining contender should enter settlement.");
            Debug.Assert(engine.State.CurrentActionSeat == -1, "No player should remain to act.");
        }

        private static void RunRaiseRequiresResponse()
        {
            var engine = PokerEngineFactory.CreateDemoHeadsUp();

            var raise = engine.Apply("local-player", PokerIntent.Raise(60));
            Debug.Assert(raise.Accepted, "Raise to 60 should be accepted.");
            Debug.Assert(engine.State.CurrentBet == 60, "Current bet should be 60.");
            Debug.Assert(engine.State.CurrentActionSeat == 1, "Opponent should respond to raise.");
            Debug.Assert(engine.State.Street == PokerStreet.Preflop, "Street cannot advance before response.");

            var call = engine.Apply("opponent-player", PokerIntent.Call());
            Debug.Assert(call.Accepted, "Opponent call should be accepted.");
            Debug.Assert(engine.State.Street == PokerStreet.Flop, "Matched raise should complete preflop.");
        }
    }
}
