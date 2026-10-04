using UnityEngine;
using SpatialPoker.Interaction;

namespace SpatialPoker.Presentation.Cards
{
    public sealed class CardEntity : MonoBehaviour, IARInteractable
    {
        [SerializeField] private string objectId;
        [SerializeField] private string ownerPlayerId;
        [SerializeField] private InteractionPolicy policy;
        [SerializeField] private Transform homeAnchor;
        [SerializeField] private float followLerp = 20f;

        private Vector3 _grabOffset;
        private bool _grabbed;

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

            if (homeAnchor != null)
                transform.SetPositionAndRotation(
                    homeAnchor.position,
                    homeAnchor.rotation);
        }
    }
}
