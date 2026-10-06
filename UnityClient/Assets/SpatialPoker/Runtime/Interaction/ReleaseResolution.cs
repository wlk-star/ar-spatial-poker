namespace SpatialPoker.Interaction
{
    public enum ReleaseOutcomeType
    {
        Snap,
        Return,
        Bet,
        Fold
    }

    public readonly struct ReleaseOutcome
    {
        public readonly ReleaseOutcomeType Type;
        public readonly SnapPoint SnapPoint;

        public ReleaseOutcome(ReleaseOutcomeType type, SnapPoint snapPoint = null)
        {
            Type = type;
            SnapPoint = snapPoint;
        }
    }
}
