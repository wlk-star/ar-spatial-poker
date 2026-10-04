using System.Collections.Generic;
using UnityEngine;

namespace SpatialPoker.Interaction
{
    public sealed class InteractionResolver : MonoBehaviour
    {
        [SerializeField] private LayerMask interactableMask = ~0;
        [Min(0.001f)]
        [SerializeField] private float probeRadius = 0.03f;
        [SerializeField] private int maxHits = 16;

        private Collider[] _hits;

        private void Awake()
        {
            _hits = new Collider[Mathf.Max(1, maxHits)];
        }

        public bool TryResolve(
            Vector3 worldPosition,
            string localPlayerId,
            out IARInteractable interactable)
        {
            interactable = null;

            var count = Physics.OverlapSphereNonAlloc(
                worldPosition,
                probeRadius,
                _hits,
                interactableMask,
                QueryTriggerInteraction.Collide);

            var bestDistance = float.PositiveInfinity;

            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit == null)
                    continue;

                var candidate = hit.GetComponentInParent<IARInteractable>();
                if (candidate == null)
                    continue;

                var policy = candidate.Policy;
                if (policy == null)
                    continue;

                if (policy.OwnerOnly &&
                    !string.IsNullOrEmpty(candidate.OwnerPlayerId) &&
                    candidate.OwnerPlayerId != localPlayerId)
                {
                    continue;
                }

                var distance = Vector3.SqrMagnitude(
                    candidate.InteractionTransform.position - worldPosition);

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                interactable = candidate;
            }

            return interactable != null;
        }
    }
}
