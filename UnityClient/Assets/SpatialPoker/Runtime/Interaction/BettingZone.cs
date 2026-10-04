using System;
using UnityEngine;
using SpatialPoker.Poker;
using SpatialPoker.Presentation.Chips;

namespace SpatialPoker.Interaction
{
    [RequireComponent(typeof(Collider))]
    public sealed class BettingZone : MonoBehaviour
    {
        public event Action<int> BetPreviewChanged;
        public event Action<PokerIntent> IntentCreated;

        [SerializeField] private int currentPreview;

        public int CurrentPreview => currentPreview;

        private void OnTriggerEnter(Collider other)
        {
            var group = other.GetComponentInParent<ChipGroup>();
            if (group == null)
                return;

            currentPreview += Mathf.Max(0, group.Value);
            BetPreviewChanged?.Invoke(currentPreview);
        }

        private void OnTriggerExit(Collider other)
        {
            var group = other.GetComponentInParent<ChipGroup>();
            if (group == null)
                return;

            currentPreview = Mathf.Max(0, currentPreview - group.Value);
            BetPreviewChanged?.Invoke(currentPreview);
        }

        public void ConfirmBet()
        {
            if (currentPreview <= 0)
                return;

            IntentCreated?.Invoke(PokerIntent.Bet(currentPreview));
        }

        public void ResetPreview()
        {
            currentPreview = 0;
            BetPreviewChanged?.Invoke(currentPreview);
        }
    }
}
