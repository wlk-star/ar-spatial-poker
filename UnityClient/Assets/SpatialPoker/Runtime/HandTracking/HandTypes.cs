using System;
using UnityEngine;

namespace SpatialPoker.HandTracking
{
    public enum HandSide
    {
        Left,
        Right
    }

    public enum HandTrackingStatus
    {
        Unavailable,
        Initializing,
        Tracking,
        Limited,
        Lost
    }

    [Serializable]
    public struct HandJoint
    {
        public Vector3 Position;
        public Quaternion Rotation;
        [Range(0f, 1f)] public float Confidence;

        public HandJoint(Vector3 position, Quaternion rotation, float confidence)
        {
            Position = position;
            Rotation = rotation;
            Confidence = Mathf.Clamp01(confidence);
        }
    }

    [Serializable]
    public struct TrackedHand
    {
        public HandSide Side;
        public bool IsTracked;
        [Range(0f, 1f)] public float Confidence;

        public HandJoint Wrist;
        public HandJoint Palm;
        public HandJoint ThumbTip;
        public HandJoint IndexTip;
        public HandJoint MiddleTip;
    }

    [Serializable]
    public struct HandFrame
    {
        public TrackedHand Left;
        public TrackedHand Right;
        public double Timestamp;
    }
}
