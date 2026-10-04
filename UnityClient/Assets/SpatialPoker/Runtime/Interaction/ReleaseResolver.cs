using UnityEngine;

namespace SpatialPoker.Interaction
{
    public sealed class ReleaseResolver : MonoBehaviour
    {
        [SerializeField] private InteractionZone[] zones;
        [SerializeField] private SnapManager snapManager;

        public ReleaseOutcome ResolveCard(
            Vector3 worldPosition,
            bool allowFold)
        {
            var zone = FindZone(worldPosition);

            if (allowFold &&
                zone != null &&
                zone.ZoneType == InteractionZoneType.Fold)
            {
                return new ReleaseOutcome(ReleaseOutcomeType.Fold);
            }

            if (snapManager != null &&
                snapManager.TryFindBest(
                    worldPosition,
                    SnapType.HoleCard,
                    out var point))
            {
                return new ReleaseOutcome(ReleaseOutcomeType.Snap, point);
            }

            return new ReleaseOutcome(ReleaseOutcomeType.Return);
        }

        public ReleaseOutcome ResolveChip(Vector3 worldPosition)
        {
            var zone = FindZone(worldPosition);

            if (zone != null &&
                zone.ZoneType == InteractionZoneType.Betting)
            {
                return new ReleaseOutcome(ReleaseOutcomeType.Bet);
            }

            if (snapManager != null &&
                snapManager.TryFindBest(
                    worldPosition,
                    SnapType.PlayerChip,
                    out var point))
            {
                return new ReleaseOutcome(ReleaseOutcomeType.Snap, point);
            }

            return new ReleaseOutcome(ReleaseOutcomeType.Return);
        }

        private InteractionZone FindZone(Vector3 worldPosition)
        {
            if (zones == null)
                return null;

            foreach (var zone in zones)
            {
                if (zone != null && zone.Contains(worldPosition))
                    return zone;
            }

            return null;
        }
    }
}
