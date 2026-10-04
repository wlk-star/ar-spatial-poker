using System;
using UnityEngine;

namespace SpatialPoker.HandTracking
{
    public abstract class HandTrackingProviderBehaviour : MonoBehaviour, IHandTrackingProvider
    {
        public abstract bool IsAvailable { get; }
        public abstract HandTrackingStatus Status { get; }

        public abstract event Action<HandTrackingStatus> StatusChanged;

        public abstract bool TryGetCurrentFrame(out HandFrame frame);
    }
}
