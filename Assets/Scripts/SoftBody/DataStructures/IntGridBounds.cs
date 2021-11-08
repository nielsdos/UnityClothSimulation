using UnityEngine;

namespace SoftBody.DataStructures
{
    /// <summary>
    /// Integer grid bounds, used for specifying the grid bounds occupied by an item in the spatial hasher.
    /// </summary>
    public readonly struct IntGridBounds
    {
        public readonly int XMinBound, YMinBound, ZMinBound;
        public readonly int XMaxBound, YMaxBound, ZMaxBound;

        public IntGridBounds(Vector3 objectCentroid, Vector3 objectSize, float gridSize)
        {
            var halfSize = objectSize * 0.5f;
            var minimum = (objectCentroid - halfSize) / gridSize;
            var maximum = (objectCentroid + halfSize) / gridSize;
            XMinBound = Mathf.FloorToInt(minimum.x);
            YMinBound = Mathf.FloorToInt(minimum.y);
            ZMinBound = Mathf.FloorToInt(minimum.z);
            XMaxBound = Mathf.FloorToInt(maximum.x);
            YMaxBound = Mathf.FloorToInt(maximum.y);
            ZMaxBound = Mathf.FloorToInt(maximum.z);
        }

        public static IntGridBounds From<T>(ISpatialHashable<T> item, float gridSize)
        {
            // Static method because constructor can't be generic without the type being generic.
            return new IntGridBounds(item.Centroid, item.Size, gridSize);
        }

        public static bool operator !=(IntGridBounds me, IntGridBounds other)
        {
            return !(me == other);
        }

        public static bool operator ==(IntGridBounds me, IntGridBounds other)
        {
            return me.XMinBound == other.XMinBound
                   && me.YMinBound == other.YMinBound
                   && me.ZMinBound == other.ZMinBound
                   && me.XMaxBound == other.XMaxBound
                   && me.YMaxBound == other.YMaxBound
                   && me.ZMaxBound == other.ZMaxBound;
        }

        public bool Equals(IntGridBounds other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is IntGridBounds other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = XMinBound;
                hashCode = (hashCode * 397) ^ YMinBound;
                hashCode = (hashCode * 397) ^ ZMinBound;
                hashCode = (hashCode * 397) ^ XMaxBound;
                hashCode = (hashCode * 397) ^ YMaxBound;
                hashCode = (hashCode * 397) ^ ZMaxBound;
                return hashCode;
            }
        }
    }
}