using UnityEngine;
using SpatialPoker.Poker;

namespace SpatialPoker.Interaction
{
    public sealed class LegalActionIntentSink : MonoBehaviour, IPokerIntentSink
    {
        [SerializeField] private LegalActionGate gate;
        [SerializeField] private MonoBehaviour downstreamSinkBehaviour;

        private IPokerIntentSink Downstream =>
            downstreamSinkBehaviour as IPokerIntentSink;

        public void Submit(in PokerIntent intent)
        {
            var action = intent.Type switch
            {
                PokerIntentType.Fold => "FOLD",
                PokerIntentType.Check => "CHECK",
                PokerIntentType.Call => "CALL",
                PokerIntentType.Bet => "BET",
                PokerIntentType.Raise => "RAISE",
                PokerIntentType.AllIn => "ALL_IN",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(action) ||
                gate == null ||
                !gate.Allows(action))
            {
                return;
            }

            if ((intent.Type == PokerIntentType.Bet ||
                 intent.Type == PokerIntentType.Raise) &&
                (intent.Amount < gate.MinRaiseTo ||
                 intent.Amount > gate.MaxRaiseTo))
            {
                return;
            }

            Downstream?.Submit(intent);
        }
    }
}
