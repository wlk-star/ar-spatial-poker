using UnityEngine;

namespace SpatialPoker.HandTracking.Gestures
{
    public enum PinchPhase
    {
        Open,
        Started,
        Held,
        Released
    }

    public sealed class PinchRecognizer
    {
        private readonly float _enterDistance;
        private readonly float _exitDistance;
        private bool _pinched;

        public PinchRecognizer(
            float enterDistance = 0.025f,
            float exitDistance = 0.035f)
        {
            _enterDistance = Mathf.Max(0.001f, enterDistance);
            _exitDistance = Mathf.Max(_enterDistance, exitDistance);
        }

        public PinchPhase Evaluate(in TrackedHand hand)
        {
            if (!hand.IsTracked)
            {
                var wasPinched = _pinched;
                _pinched = false;
                return wasPinched ? PinchPhase.Released : PinchPhase.Open;
            }

            var distance = Vector3.Distance(
                hand.ThumbTip.Position,
                hand.IndexTip.Position);

            if (!_pinched && distance <= _enterDistance)
            {
                _pinched = true;
                return PinchPhase.Started;
            }

            if (_pinched && distance >= _exitDistance)
            {
                _pinched = false;
                return PinchPhase.Released;
            }

            return _pinched ? PinchPhase.Held : PinchPhase.Open;
        }
    }
}
