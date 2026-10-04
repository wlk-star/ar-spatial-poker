using UnityEngine;
using SpatialPoker.Interaction;
using SpatialPoker.Poker;

namespace SpatialPoker.Debugging
{
    public sealed class PokerIntentDebugSink : MonoBehaviour, IPokerIntentSink
    {
        [SerializeField] private BettingZone bettingZone;

        private void OnEnable()
        {
            if (bettingZone != null)
                bettingZone.IntentCreated += Submit;
        }

        private void OnDisable()
        {
            if (bettingZone != null)
                bettingZone.IntentCreated -= Submit;
        }

        public void Submit(in PokerIntent intent)
        {
            Debug.Log(
                $"[SpatialPoker] Intent={intent.Type}, Amount={intent.Amount}",
                this);
        }
    }
}
