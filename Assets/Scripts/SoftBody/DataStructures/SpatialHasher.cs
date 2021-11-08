using System.Collections.Generic;
using UnityEngine;

namespace SoftBody.DataStructures
{
    /// <summary>
    /// Spatial hashing implementation for a type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SpatialHasher<T>
    {
        /// <summary>
        /// Query sphere for nearby items.
        /// </summary>
        public struct Query
        {
            public Vector3 Centroid;
            public float Radius;
        }

        private readonly float _gridSize;
        private readonly Dictionary<Vector3Int, List<SpatialHashingItemTracker<T>>> _grid;

        /// <summary>
        /// Creates a new Spatial Hashing grid with each grid being a cube with the provided grid size.
        /// </summary>
        /// <param name="gridSize">Size in each axis of the cube.</param>
        public SpatialHasher(float gridSize)
        {
            _gridSize = gridSize;
            _grid = new Dictionary<Vector3Int, List<SpatialHashingItemTracker<T>>>();
        }

        /// <summary>
        /// Inserts a new spatial hashable item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="bounds">The size bounds.</param>
        private void Insert(ISpatialHashable<T> item, IntGridBounds bounds)
        {
            var itemTracker = item.ShItemTracker;
            itemTracker.Indices.Clear();
            itemTracker.CurrentBounds = bounds;

            for (var x = bounds.XMinBound; x <= bounds.XMaxBound; ++x)
            for (var y = bounds.YMinBound; y <= bounds.YMaxBound; ++y)
            for (var z = bounds.ZMinBound; z <= bounds.ZMaxBound; ++z)
            {
                var key = new Vector3Int(x, y, z);

                if (!_grid.TryGetValue(key, out var list))
                    _grid.Add(key, list = new List<SpatialHashingItemTracker<T>>());

                itemTracker.Indices.Add(list, list.Count);
                list.Add(itemTracker);
            }
        }

        /// <summary>
        /// Inserts a new spatial hashable item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Insert(ISpatialHashable<T> item)
        {
            var bounds = IntGridBounds.From(item, _gridSize);
            Insert(item, bounds);
        }

        /// <summary>
        /// Updates a spatial hashable item so it is now located at its new location.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Update(ISpatialHashable<T> item)
        {
            var oldBounds = item.ShItemTracker.CurrentBounds;
            var bounds = IntGridBounds.From(item, _gridSize);
            if (oldBounds == bounds)
                return;
            Remove(item);
            Insert(item);
        }

        /// <summary>
        /// Removes a spatial hashable item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Remove(ISpatialHashable<T> item)
        {
            var itemTracker = item.ShItemTracker;

            foreach (var pair in itemTracker.Indices)
            {
                var list = pair.Key;
                var itemListIndex = pair.Value;

                // Removing in O(1): "swap" item to remove with last and update the indices.
                var lastListIndex = list.Count - 1;
                // If we swap with ourselves, this would modify our own collection.
                // However, we can simply ignore this since it will be removed anyway.
                if (lastListIndex != itemListIndex)
                {
                    var lastItemInList = list[lastListIndex];
                    list[itemListIndex] = lastItemInList;
                    lastItemInList.Indices[list] = itemListIndex;
                }

                list.RemoveAt(lastListIndex);
            }

            itemTracker.Indices.Clear();
        }

        /// <summary>
        /// Internal method to enumerate all nearby items to a given bounding box.
        /// </summary>
        /// <param name="bounds">The bounding box.</param>
        /// <returns>An iterator for all items in the grids overlapping with the bounding box.</returns>
        private IEnumerable<T> EnumerateNear(IntGridBounds bounds)
        {
            for (var x = bounds.XMinBound; x <= bounds.XMaxBound; ++x)
            for (var y = bounds.YMinBound; y <= bounds.YMaxBound; ++y)
            for (var z = bounds.ZMinBound; z <= bounds.ZMaxBound; ++z)
            {
                var key = new Vector3Int(x, y, z);
                if (_grid.TryGetValue(key, out var list))
                    foreach (var listItem in list)
                        yield return listItem.Item;
            }
        }

        /// <summary>
        /// Enumerate all nearby items to a given item. Includes itself.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>An iterator for all nearby items.</returns>
        public IEnumerable<T> EnumerateNear(ISpatialHashable<T> item)
        {
            foreach (var list in item.ShItemTracker.Indices.Keys)
                // A foreach here generates too much garbage because an enumerator is constructed on the heap.
                for (var i = 0; i < list.Count; ++i)
                    yield return list[i].Item;
        }

        /// <summary>
        /// Enumerate all items close to a query sphere.
        /// </summary>
        /// <param name="query">The query sphere.</param>
        /// <returns>An iterator for all nearby items.</returns>
        public IEnumerable<T> EnumerateNear(Query query)
        {
            return EnumerateNear(new IntGridBounds(query.Centroid,
                new Vector3(query.Radius, query.Radius, query.Radius),
                _gridSize));
        }
    }
}