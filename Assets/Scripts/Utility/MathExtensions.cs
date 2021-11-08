using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
    /// <summary>
    /// Mathematics utility functions
    /// </summary>
    public static class MathExtensions
    {
        /// <summary>
        /// Clamps the components of a vector between bounds.
        /// </summary>
        /// <param name="value">The vector to clamp.</param>
        /// <param name="min">The lower bounds.</param>
        /// <param name="max">The upper bounds.</param>
        /// <returns>The clamped vector.</returns>
        public static Vector3 Clamp(this Vector3 value, Vector3 min, Vector3 max)
        {
            // The old implementation used `Vector3.Min` in combination with `Vector3.Max`
            // to clamp the vector. Even though this only constructs temporary stack values, 
            // the Unity runtime still was impacted with performance.
            // This manual implementation is way faster (from ~3% to <1% in profile graph).
            Vector3 result;
            result.x = Mathf.Clamp(value.x, min.x, max.x);
            result.y = Mathf.Clamp(value.y, min.y, max.y);
            result.z = Mathf.Clamp(value.z, min.z, max.z);
            return result;
        }

        /// <summary>
        /// Sorts a tuple such that the first element is the smallest and the second element is the largest.
        /// </summary>
        /// <param name="a">An element of the tuple.</param>
        /// <param name="b">An element of the tuple.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The sorted tuple.</returns>
        public static (T, T) SortTuple<T>(T a, T b) where T : IComparable
        {
            return Comparer<T>.Default.Compare(a, b) <= 0 ? (a, b) : (b, a);
        }
    }
}