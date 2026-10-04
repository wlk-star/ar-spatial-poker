using System;
using System.Collections.Generic;
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

        [SerializeField] private bool autoConfirmOnRelease = true;

        private readonly HashSet<ChipGroup> _groups = new();
        private int _currentPreview;

        public int CurrentPreview => _currentPreview;

        private void OnTriggerEnter(Collider other)
        {
            var group = FindChipGroup(other);
            if (group == null || !_groups.Add(group))
                return;

            group.Released += OnChipReleased;
            RecalculatePreview();
        }

        private void OnTriggerExit(Collider other)
        {
            var group = FindChipGroup(other);
            if (group == null || !_groups.Remove(group))
                return;

            group.Released -= OnChipReleased;
            RecalculatePreview();
        }

        private void OnDisable()
        {
            foreach (var group in _groups)
            {
                if (group != null)
                    group.Released -= OnChipReleased;
            }

            _groups.Clear();
            SetPreview(0);
        }

        public void ConfirmBet()
        {
            if (_currentPreview <= 0)
                return;

            IntentCreated?.Invoke(PokerIntent.Bet(_currentPreview));
        }

        public void ResetPreview()
        {
            foreach (var group in _groups)
            {
                if (group != null)
                    group.Released -= OnChipReleased;
            }

            _groups.Clear();
            SetPreview(0);
        }

        private void OnChipReleased(ChipGroup group)
        {
            if (autoConfirmOnRelease && _groups.Contains(group))
                ConfirmBet();
        }

        private void RecalculatePreview()
        {
            var total = 0;

            foreach (var group in _groups)
            {
                if (group != null)
                    total += Mathf.Max(0, group.Value);
            }

            SetPreview(total);
        }

        private void SetPreview(int amount)
        {
            if (_currentPreview == amount)
                return;

            _currentPreview = amount;
            BetPreviewChanged?.Invoke(_currentPreview);
        }

        private static ChipGroup FindChipGroup(Collider collider)
        {
            var behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
            {
                if (behaviour is ChipGroup group)
                    return group;
            }

            return null;
        }
    }
}
