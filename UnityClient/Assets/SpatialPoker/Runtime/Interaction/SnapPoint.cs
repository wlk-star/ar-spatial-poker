using UnityEngine;

namespace SpatialPoker.Interaction
{
    public enum SnapType
    {
        HoleCard,
        PlayerChip,
        Betting,
        Pot,
        Board,
        Dealer
    }

    public sealed class SnapPoint : MonoBehaviour
    {
        [SerializeField] private string snapId;
        [SerializeField] private SnapType snapType;
        [Min(0.001f)]
        [SerializeField] private float radius = 0.08f;
        [SerializeField] private int priority;

        public string SnapId => snapId;
        public SnapType SnapType => snapType;
        public float Radius => radius;
        public int Priority => priority;
    }
}
