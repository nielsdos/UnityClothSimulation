using System.Collections.Generic;

namespace SoftBody.DataStructures
{
    /// <summary>
    /// This class helps the SpatialHasher to quickly perform its updates by keeping track of which index is used where.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SpatialHashingItemTracker<T>
    {
        public readonly Dictionary<List<SpatialHashingItemTracker<T>>, int> Indices;
        public IntGridBounds CurrentBounds;
        public readonly T Item;

        public SpatialHashingItemTracker(T item)
        {
            Indices = new Dictionary<List<SpatialHashingItemTracker<T>>, int>();
            Item = item;
        }
    }
}