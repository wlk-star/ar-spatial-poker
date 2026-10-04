using System.Collections;
using UnityEngine;
using SpatialPoker.Interaction;
using SpatialPoker.Poker;

namespace SpatialPoker.Presentation.Cards
{
    public sealed class CardEntity : MonoBehaviour, IARInteractable
    {
        [SerializeField] private string objectId;
        [SerializeField] private string ownerPlayerId;
        [SerializeField] private InteractionPolicy policy;
        [SerializeField] private Transform homeAnchor;
        [SerializeField] private ReleaseResolver releaseResolver;
        [SerializeField] private MonoBehaviour pokerIntentSinkBehaviour;
        [SerializeField] private bool allowFoldGesture = true;
        [SerializeField] private float followLerp = 20f;
        [Min(0.01f)]
        [SerializeField] private float returnDuration = 0.18f;

        private Vector3 _grabOffset;
        private bool _grabbed;
        private Coroutine _returnRoutine;

        private IPokerIntentSink IntentSink =>
            pokerIntentSinkBehaviour as IPokerIntentSink;

        public string ObjectId => objectId;
        public string OwnerPlayerId => ownerPlayerId;
        public InteractionPolicy Policy => policy;
        public Transform InteractionTransform => transform;

        public void OnHoverEnter(in InteractionContext context) { }
        public void OnHoverExit(in InteractionContext context) { }
        public void OnTouch(in InteractionContext context) { }

        public GrabResult TryGrab(in InteractionContext context)
        {
            if (policy == null || policy.GrabMode == GrabMode.Disabled)
                return GrabResult.Rejected;

            if (_returnRoutine != null)
            {
                StopCoroutine(_returnRoutine);
                _returnRoutine = null;
            }

            _grabOffset = transform.position - context.WorldPosition;
            _grabbed = true;

            return policy.GrabMode == GrabMode.Cosmetic
                ? GrabResult.Cosmetic
                : GrabResult.Accepted;
        }

        public void OnGrabMove(in InteractionContext context)
        {
            if (!_grabbed || policy == null)
                return;

            var target = context.WorldPosition + _grabOffset;

            if (policy.GrabMode == GrabMode.Cosmetic && homeAnchor != null)
            {
                var delta = target - homeAnchor.position;
                target = homeAnchor.position +
                         Vector3.ClampMagnitude(delta, policy.MaxCosmeticGrabDistance);
            }

            transform.position = Vector3.Lerp(
                transform.position,
                target,
                Time.deltaTime * followLerp);
        }

        public void OnRelease(in InteractionContext context)
        {
            _grabbed = false;

            if (policy != null && policy.GrabMode == GrabMode.Cosmetic)
            {
                StartReturn(homeAnchor);
                return;
            }

            if (releaseResolver == null)
            {
                StartReturn(homeAnchor);
                return;
            }

            var outcome = releaseResolver.ResolveCard(
                transform.position,
                allowFoldGesture);

            switch (outcome.Type)
            {
                case ReleaseOutcomeType.Fold:
                    IntentSink?.Submit(PokerIntent.Fold());
                    StartReturn(homeAnchor);
                    break;

                case ReleaseOutcomeType.Snap:
                    StartReturn(outcome.SnapPoint != null
                        ? outcome.SnapPoint.transform
                        : homeAnchor);
                    break;

                default:
                    StartReturn(homeAnchor);
                    break;
            }
        }

        private void StartReturn(Transform target)
        {
            if (target == null)
                return;

            if (_returnRoutine != null)
                StopCoroutine(_returnRoutine);

            _returnRoutine = StartCoroutine(ReturnTo(target));
        }

        private IEnumerator ReturnTo(Transform target)
        {
            var startPosition = transform.position;
            var startRotation = transform.rotation;
            var targetPosition = target.position;
            var targetRotation = target.rotation;
            var elapsed = 0f;

            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / returnDuration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);

                transform.position = Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    eased);

                transform.rotation = Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    eased);

                yield return null;
            }

            transform.SetPositionAndRotation(
                targetPosition,
                targetRotation);

            _returnRoutine = null;
        }
    }
}
