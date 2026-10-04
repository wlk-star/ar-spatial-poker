using UnityEngine;
using SpatialPoker.Poker;

namespace SpatialPoker.Interaction
{
    /// <summary>
    /// Shared entry point for table-tap gestures and reliable 2D fallback UI.
    /// Route this through PokerIntentNormalizer/LegalActionIntentSink.
    /// </summary>
    public sealed class PokerActionBridge : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour intentSinkBehaviour;
        [SerializeField] private LegalActionGate gate;

        private IPokerIntentSink Sink =>
            intentSinkBehaviour as IPokerIntentSink;

        public void TryCheck()
        {
            if (gate != null && gate.Allows("CHECK"))
                Sink?.Submit(PokerIntent.Check());
        }

        public void TryFold()
        {
            if (gate != null && gate.Allows("FOLD"))
                Sink?.Submit(PokerIntent.Fold());
        }

        public void TryCall()
        {
            if (gate != null && gate.Allows("CALL"))
                Sink?.Submit(PokerIntent.Call());
        }

        public void TryAllIn()
        {
            if (gate != null && gate.Allows("ALL_IN"))
                Sink?.Submit(PokerIntent.AllIn(gate.MaxRaiseTo));
        }

        public void TryBetOrRaise(int raiseToAmount)
        {
            Sink?.Submit(PokerIntent.Bet(raiseToAmount));
        }
    }
}
