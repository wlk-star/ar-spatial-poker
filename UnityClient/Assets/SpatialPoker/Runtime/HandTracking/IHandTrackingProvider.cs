using System;

namespace SpatialPoker.HandTracking
{
    public interface IHandTrackingProvider
    {
        bool IsAvailable { get; }
        HandTrackingStatus Status { get; }

        event Action<HandTrackingStatus> StatusChanged;

        bool TryGetCurrentFrame(out HandFrame frame);
    }
}
