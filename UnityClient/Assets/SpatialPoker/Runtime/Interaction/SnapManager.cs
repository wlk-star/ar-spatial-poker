using System.Collections.Generic;
using UnityEngine;

namespace SpatialPoker.Interaction
{
    public sealed class SnapManager : MonoBehaviour
    {
        private readonly List<SnapPoint> _points = new();

        public void Register(SnapPoint point)
        {
            if (point != null && !_points.Contains(point))
                _points.Add(point);
        }

        public void Unregister(SnapPoint point)
        {
            if (point != null)
                _points.Remove(point);
        }

        public bool TryFindBest(
            Vector3 worldPosition,
            SnapType type,
            out SnapPoint best)
        {
            best = null;
            var bestPriority = int.MinValue;
            var bestDistance = float.PositiveInfinity;

            foreach (var point in _points)
            {
                if (point == null || point.SnapType != type)
                    continue;

                var distance = Vector3.Distance(
                    worldPosition,
                    point.transform.position);

                if (distance > point.Radius)
                    continue;

                if (point.Priority < bestPriority)
                    continue;

                if (point.Priority == bestPriority && distance >= bestDistance)
                    continue;

                best = point;
                bestPriority = point.Priority;
                bestDistance = distance;
            }

            return best != null;
        }
    }
}
