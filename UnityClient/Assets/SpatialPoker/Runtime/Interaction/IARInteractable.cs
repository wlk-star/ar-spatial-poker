using UnityEngine;

namespace SpatialPoker.Interaction
{
    public readonly struct InteractionContext
    {
        public readonly string LocalPlayerId;
        public readonly Vector3 WorldPosition;
        public readonly Vector3 WorldDirection;

        public InteractionContext(
            string localPlayerId,
            Vector3 worldPosition,
            Vector3 worldDirection)
        {
            LocalPlayerId = localPlayerId;
            WorldPosition = worldPosition;
            WorldDirection = worldDirection;
        }
    }

    public enum GrabResult
    {
        Rejected,
        Accepted,
        Cosmetic
    }

    public interface IARInteractable
    {
        string ObjectId { get; }
        string OwnerPlayerId { get; }
        InteractionPolicy Policy { get; }
        Transform InteractionTransform { get; }

        void OnHoverEnter(in InteractionContext context);
        void OnHoverExit(in InteractionContext context);
        void OnTouch(in InteractionContext context);
        GrabResult TryGrab(in InteractionContext context);
        void OnGrabMove(in InteractionContext context);
        void OnRelease(in InteractionContext context);
    }
}
