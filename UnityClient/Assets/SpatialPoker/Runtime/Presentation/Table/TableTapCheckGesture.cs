using UnityEngine;
using SpatialPoker.Interaction;

namespace SpatialPoker.Presentation.Table
{
    public sealed class TableTapCheckGesture : MonoBehaviour
    {
        [SerializeField] private PokerActionBridge actionBridge;
        [SerializeField] private float maxTapDuration = 0.35f;
        [SerializeField] private float cooldown = 0.4f;

        private float _lastTriggerTime = -10f;

        public void NotifyTableTap(float duration)
        {
            if (duration > maxTapDuration)
                return;

            if (Time.unscaledTime - _lastTriggerTime < cooldown)
                return;

            _lastTriggerTime = Time.unscaledTime;
            actionBridge?.TryCheck();
        }
    }
}
