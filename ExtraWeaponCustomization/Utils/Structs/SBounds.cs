using System;
using System.Numerics;

namespace EWC.Utils.Structs
{
    // System-based UnityEngine.Bounds equivalent
    public struct SBounds
    {
        public Vector3 center;
        public Vector3 extents;
        public Vector3 max;
        public Vector3 min;
        public Vector3 size;

        public SBounds(UnityEngine.Bounds bounds)
        {
            center = new(bounds.center.x, bounds.center.y, bounds.center.z);
            size = new(bounds.size.x, bounds.size.y, bounds.size.z);
            extents = size * 0.5f;
            max = center + extents;
            min = center - extents;
        }

        public readonly Vector3 ClosestPoint(Vector3 point)
        {
            return new(
                Math.Clamp(point.X, min.X, max.X),
                Math.Clamp(point.Y, min.Y, max.Y),
                Math.Clamp(point.Z, min.Z, max.Z)
                );
        }

        public readonly bool Contains(Vector3 point)
        {
            return point.X >= min.X && point.X <= max.X &&
                   point.Y >= min.Y && point.Y <= max.Y &&
                   point.Z >= min.Z && point.Z <= max.Z;
        }

        public static implicit operator SBounds(UnityEngine.Bounds bounds) => new(bounds);
    }
}
