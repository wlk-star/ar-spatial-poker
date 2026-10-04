using UnityEngine;
using SpatialPoker.Poker;
using SpatialPoker.Poker.Domain;

namespace SpatialPoker.Debugging
{
    public sealed class PokerEngineDebugController : MonoBehaviour, IPokerIntentSink
    {
        [SerializeField] private string localPlayerId = "local-player";

        private PokerEngine _engine;

        private void Awake()
        {
            _engine = PokerEngineFactory.CreateDemoHeadsUp();
            LogState("Initialized");
        }

        public void Submit(in PokerIntent intent)
        {
            var result = _engine.Apply(localPlayerId, intent);

            Debug.Log(
                $"[PokerEngine] {intent.Type} amount={intent.Amount} accepted={result.Accepted} error={result.Error}",
                this);

            LogState("After action");
        }

        private void LogState(string prefix)
        {
            var state = _engine.State;
            Debug.Log(
                $"[PokerEngine] {prefix}: street={state.Street}, pot={state.Pot}, currentBet={state.CurrentBet}, currentSeat={state.CurrentActionSeat}",
                this);
        }
    }
}
