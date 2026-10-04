using System;
using UnityEngine;
using SpatialPoker.Interaction;

namespace SpatialPoker.Presentation.Chips
{
    public sealed class ChipGroup : MonoBehaviour, IARInteractable
    {
        [SerializeField] private string objectId;
        [SerializeField] private string ownerPlayerId;
        [SerializeField] private InteractionPolicy policy;
        [SerializeField] private int value = 100;
        [SerializeField] private Transform homeAnchor;
        [SerializeField] private ReleaseResolver releaseResolver;
        [SerializeField] private float followLerp = 20f;

        private Vector3 _grabOffset;
        private bool _grabbed;

        public event Action<ChipGroup> Released;

        public int Value => value;
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
            return GrabResult.Accepted;
        }

        public void OnGrabMove(in InteractionContext context)
        {
            if (!_grabbed)
                return;

            var target = context.WorldPosition + _grabOffset;
            transform.position = Vector3.Lerp(
                transform.position,
                target,
                Time.deltaTime * followLerp);
        }

        public void OnRelease(in InteractionContext context)
        {
            _grabbed = false;

            if (releaseResolver != null)
            {
                var outcome = releaseResolver.ResolveChip(transform.position);

                switch (outcome.Type)
                {
                    case ReleaseOutcomeType.Snap:
                        if (outcome.SnapPoint != null)
                        {
                            transform.SetPositionAndRotation(
                                outcome.SnapPoint.transform.position,
                                outcome.SnapPoint.transform.rotation);
                        }
                        break;

                    case ReleaseOutcomeType.Return:
                        ReturnHome();
                        break;

                    case ReleaseOutcomeType.Bet:
                        break;
                }
            }

            Released?.Invoke(this);
        }

        public void ReturnHome()
        {
            if (homeAnchor != null)
                transform.SetPositionAndRotation(
                    homeAnchor.position,
                    homeAnchor.rotation);
        }
    }
}
