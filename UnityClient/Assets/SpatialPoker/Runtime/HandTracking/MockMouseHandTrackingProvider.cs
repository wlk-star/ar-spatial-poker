using System;
using UnityEngine;

namespace SpatialPoker.HandTracking
{
    /// <summary>
    /// Editor-friendly provider used to validate the interaction pipeline before
    /// a native mobile hand tracker is integrated.
    /// Left mouse button acts as pinch; mouse position is projected onto a
    /// configurable interaction plane.
    /// </summary>
    public sealed class MockMouseHandTrackingProvider : HandTrackingProviderBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private Transform interactionPlane;
        [SerializeField] private float openFingerDistance = 0.06f;
        [SerializeField] private float pinchedFingerDistance = 0.01f;

        public override bool IsAvailable => interactionCamera != null && interactionPlane != null;
        public override HandTrackingStatus Status =>
            IsAvailable ? HandTrackingStatus.Tracking : HandTrackingStatus.Unavailable;

        public override event Action<HandTrackingStatus> StatusChanged
        {
            add { }
            remove { }
        }

        public override bool TryGetCurrentFrame(out HandFrame frame)
        {
            frame = default;
            if (!IsAvailable)
                return false;

            var ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(interactionPlane.up, interactionPlane.position);

            if (!plane.Raycast(ray, out var enter))
                return false;

            var pointer = ray.GetPoint(enter);
            var pinch = Input.GetMouseButton(0);
            var fingerDistance = pinch ? pinchedFingerDistance : openFingerDistance;
            var half = fingerDistance * 0.5f;

            var thumb = pointer - interactionCamera.transform.right * half;
            var index = pointer + interactionCamera.transform.right * half;

            var hand = new TrackedHand
            {
                Side = HandSide.Right,
                IsTracked = true,
                Confidence = 1f,
                Palm = new HandJoint(pointer, Quaternion.identity, 1f),
                Wrist = new HandJoint(pointer, Quaternion.identity, 1f),
                ThumbTip = new HandJoint(thumb, Quaternion.identity, 1f),
                IndexTip = new HandJoint(index, Quaternion.identity, 1f),
                MiddleTip = new HandJoint(pointer, Quaternion.identity, 1f)
            };

            frame = new HandFrame
            {
                Right = hand,
                Timestamp = Time.realtimeSinceStartupAsDouble
            };

            return true;
        }
    }
}
