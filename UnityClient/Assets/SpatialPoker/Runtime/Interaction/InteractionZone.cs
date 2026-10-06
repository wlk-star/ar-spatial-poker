using UnityEngine;

namespace SpatialPoker.Interaction
{
    public enum InteractionZoneType
    {
        PlayerCards,
        PlayerChips,
        Betting,
        Fold,
        Public,
        Pot
    }

    [RequireComponent(typeof(Collider))]
    public sealed class InteractionZone : MonoBehaviour
    {
        [SerializeField] private InteractionZoneType zoneType;
        [SerializeField] private string ownerPlayerId;

        public InteractionZoneType ZoneType => zoneType;
        public string OwnerPlayerId => ownerPlayerId;

        public bool Contains(Vector3 worldPosition)
        {
            var collider = GetComponent<Collider>();
            return collider != null && collider.bounds.Contains(worldPosition);
        }
    }
}
