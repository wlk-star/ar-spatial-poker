using UnityEngine;
using SpatialPoker.HandTracking;
using SpatialPoker.HandTracking.Gestures;

namespace SpatialPoker.Interaction
{
    /// <summary>
    /// Connects a hand tracking provider to the generic interaction layer.
    /// This class knows about gestures and interactables, but not poker rules.
    /// </summary>
    public sealed class GestureInteractionController : MonoBehaviour
    {
        [SerializeField] private HandTrackingProviderBehaviour provider;
        [SerializeField] private InteractionResolver resolver;
        [SerializeField] private string localPlayerId = "local-player";
        [SerializeField] private HandSide handSide = HandSide.Right;
        [SerializeField] private float pinchEnterDistance = 0.025f;
        [SerializeField] private float pinchExitDistance = 0.035f;

        private PinchRecognizer _pinch;
        private IARInteractable _hovered;
        private IARInteractable _grabbed;

        private void Awake()
        {
            _pinch = new PinchRecognizer(
                pinchEnterDistance,
                pinchExitDistance);
        }

        private void Update()
        {
            if (provider == null ||
                resolver == null ||
                !provider.TryGetCurrentFrame(out var frame))
            {
                ClearHover();
                return;
            }

            var hand = handSide == HandSide.Left ? frame.Left : frame.Right;
            if (!hand.IsTracked)
            {
                ClearHover();
                return;
            }

            var pointer = hand.IndexTip.Position;
            var direction = hand.IndexTip.Rotation * Vector3.forward;
            var context = new InteractionContext(
                localPlayerId,
                pointer,
                direction);

            if (_grabbed != null)
            {
                _grabbed.OnGrabMove(context);
            }
            else
            {
                UpdateHover(context);
            }

            switch (_pinch.Evaluate(hand))
            {
                case PinchPhase.Started:
                    TryBeginGrab(context);
                    break;

                case PinchPhase.Released:
                    EndGrab(context);
                    break;
            }
        }

        private void UpdateHover(in InteractionContext context)
        {
            resolver.TryResolve(
                context.WorldPosition,
                localPlayerId,
                out var candidate);

            if (ReferenceEquals(candidate, _hovered))
                return;

            ClearHover();

            _hovered = candidate;
            if (_hovered?.Policy != null && _hovered.Policy.CanHover)
                _hovered.OnHoverEnter(context);
        }

        private void ClearHover()
        {
            if (_hovered == null)
                return;

            var context = new InteractionContext(
                localPlayerId,
                _hovered.InteractionTransform.position,
                Vector3.forward);

            _hovered.OnHoverExit(context);
            _hovered = null;
        }

        private void TryBeginGrab(in InteractionContext context)
        {
            if (_grabbed != null)
                return;

            if (_hovered == null)
                resolver.TryResolve(
                    context.WorldPosition,
                    localPlayerId,
                    out _hovered);

            if (_hovered == null)
                return;

            var result = _hovered.TryGrab(context);
            if (result == GrabResult.Rejected)
                return;

            _grabbed = _hovered;
            _hovered = null;
        }

        private void EndGrab(in InteractionContext context)
        {
            if (_grabbed == null)
                return;

            _grabbed.OnRelease(context);
            _grabbed = null;
        }
    }
}
