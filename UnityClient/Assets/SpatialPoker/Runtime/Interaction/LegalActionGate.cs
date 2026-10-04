using System;
using UnityEngine;
using SpatialPoker.Networking;

namespace SpatialPoker.Interaction
{
    public sealed class LegalActionGate : MonoBehaviour
    {
        [SerializeField] private GameStateSynchronizer synchronizer;

        public event Action Changed;

        private void OnEnable()
        {
            if (synchronizer != null)
                synchronizer.Store.Changed += OnStoreChanged;
        }

        private void OnDisable()
        {
            if (synchronizer != null)
                synchronizer.Store.Changed -= OnStoreChanged;
        }

        private void OnStoreChanged() => Changed?.Invoke();

        public bool Allows(string action)
        {
            var actions = synchronizer?.Store.Private?.legalActions?.actions;
            if (actions == null) return false;

            for (var i = 0; i < actions.Length; i++)
            {
                if (string.Equals(actions[i], action, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public int CallAmount =>
            synchronizer?.Store.Private?.legalActions?.callAmount ?? 0;

        public int MinRaiseTo =>
            synchronizer?.Store.Private?.legalActions?.minRaiseTo ?? 0;

        public int MaxRaiseTo =>
            synchronizer?.Store.Private?.legalActions?.maxRaiseTo ?? 0;
    }
}
