using UnityEngine;
using SpatialPoker.Poker;

namespace SpatialPoker.Interaction
{
    /// <summary>
    /// Converts physical interaction intent into the currently valid poker
    /// semantic action. Example: pushing chips may be BET on an unopened
    /// street, but RAISE when a bet already exists.
    /// </summary>
    public sealed class PokerIntentNormalizer : MonoBehaviour, IPokerIntentSink
    {
        [SerializeField] private LegalActionGate gate;
        [SerializeField] private MonoBehaviour downstreamSinkBehaviour;

        private IPokerIntentSink Downstream =>
            downstreamSinkBehaviour as IPokerIntentSink;

        public void Submit(in PokerIntent intent)
        {
            if (gate == null || Downstream == null)
                return;

            var normalized = intent;

            if (intent.Type == PokerIntentType.Bet)
            {
                if (gate.Allows("BET"))
                {
                    normalized = PokerIntent.Bet(intent.Amount);
                }
                else if (gate.Allows("RAISE"))
                {
                    normalized = PokerIntent.Raise(intent.Amount);
                }
                else
                {
                    return;
                }
            }

            if (intent.Type == PokerIntentType.Call && !gate.Allows("CALL"))
                return;

            if (intent.Type == PokerIntentType.Check && !gate.Allows("CHECK"))
                return;

            if (intent.Type == PokerIntentType.Fold && !gate.Allows("FOLD"))
                return;

            if (intent.Type == PokerIntentType.AllIn && !gate.Allows("ALL_IN"))
                return;

            if ((normalized.Type == PokerIntentType.Bet ||
                 normalized.Type == PokerIntentType.Raise) &&
                (normalized.Amount < gate.MinRaiseTo ||
                 normalized.Amount > gate.MaxRaiseTo))
            {
                return;
            }

            Downstream.Submit(normalized);
        }
    }
}
