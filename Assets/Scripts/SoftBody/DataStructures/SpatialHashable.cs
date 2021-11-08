using UnityEngine;

namespace SoftBody.DataStructures
{
    /// <summary>
    /// Base interface for all classes that are able to be used as an item in the spatial hasher.
    /// </summary>
    /// <typeparam name="T">Type of the item.</typeparam>
    public interface ISpatialHashable<T>
    {
        Vector3 Centroid { get; }
        Vector3 Size { get; }
        SpatialHashingItemTracker<T> ShItemTracker { get; }
    }
}