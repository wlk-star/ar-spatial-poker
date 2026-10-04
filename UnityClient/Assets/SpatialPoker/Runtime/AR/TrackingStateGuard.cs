using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace SpatialPoker.AR
{
    /// <summary>
    /// Exposes AR session availability without touching poker state.
    /// UI can subscribe and show recalibration/fallback controls.
    /// </summary>
    public sealed class TrackingStateGuard : MonoBehaviour
    {
        public event Action<bool> TrackingAvailabilityChanged;

        private bool _lastAvailable;

        private void OnEnable()
        {
            ARSession.stateChanged += OnStateChanged;
            Publish(ARSession.state);
        }

        private void OnDisable()
        {
            ARSession.stateChanged -= OnStateChanged;
        }

        private void OnStateChanged(ARSessionStateChangedEventArgs args)
        {
            Publish(args.state);
        }

        private void Publish(ARSessionState state)
        {
            var available =
                state == ARSessionState.SessionTracking ||
                state == ARSessionState.Ready;

            if (available == _lastAvailable)
                return;

            _lastAvailable = available;
            TrackingAvailabilityChanged?.Invoke(available);
        }
    }
}
