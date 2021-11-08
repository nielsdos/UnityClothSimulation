using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utility;

namespace SoftBody
{
    /// <summary>
    /// Utility class that tracks vertex information using means.
    /// </summary>
    public sealed class MeanVertexInfoTracker
    {
        public readonly List<Vector3> MeanPositions = new List<Vector3>();
        public readonly List<Vector3> MeanTangents = new List<Vector3>();
        public readonly List<Vector3> MeanNormals = new List<Vector3>();

        /// <param name="mesh">The source mesh.</param>
        /// <param name="localScaledVertices">The locally scaled vertices according to scale and axis distance rescaling.</param>
        /// <param name="axisScaling">The axis scaling for distance calculation.</param>
        /// <param name="minimumGroupDistance">The minimum distance required between vertices to group together vertices.</param>
        /// <param name="maxParticles">Maximum amount of particles.</param>
        public MeanVertexInfoTracker(Mesh mesh, Vector3[] localScaledVertices, Vector3 axisScaling,
            float minimumGroupDistance, int maxParticles)
        {
            var tangents = new List<Vector4>();
            var normals = new List<Vector3>();
            mesh.GetTangents(tangents);
            mesh.GetNormals(normals);

            var todo = new HashSet<(Vector3, int)>();

            for (var i = 0; i < localScaledVertices.Length; ++i) todo.Add((localScaledVertices[i], i));

            // Attempt to select a vertex, create sphere around the vertex and repeat until no
            // spheres can be created or a maximum number of spheres is reached.
            while (maxParticles > 0 && todo.Count > 0)
            {
                var (sphereCentroid, _) = todo.First();
                var sphereRadius = minimumGroupDistance;

                var meanNormal = Vector3.zero;
                var meanTangent = Vector3.zero;
                var count = todo.RemoveWhere(pair =>
                {
                    var (position, index) = pair;
                    var condition = Vector3.Scale(position - sphereCentroid, axisScaling).sqrMagnitude <=
                                    sphereRadius * sphereRadius;
                    if (condition)
                    {
                        meanNormal += normals[index];
                        var tangent = tangents[index];
                        meanTangent += new Vector3(tangent.x, tangent.y, tangent.z);
                    }

                    return condition;
                });

                meanNormal /= count;
                meanTangent /= count;

                // We want a position on a model vertex such that the spring more closely resembles the model.
                // But we want to keep the normals and tangents more aggregated because we will be using it
                // as a summary of the area when we make connections.
                MeanPositions.Add(sphereCentroid);
                MeanNormals.Add(meanNormal);
                MeanTangents.Add(meanTangent);

                --maxParticles;
            }
        }

        /// <summary>
        /// Get the closest (index, squared distance) tuple to a given vertex where the index fulfills a predicate.
        /// </summary>
        /// <param name="vector">The vector to measure against.</param>
        /// <param name="indexPredicate">The index predicate.</param>
        /// <returns>The (index, squared distance) tuple.</returns>
        public (int, float) GetClosestTo(Vector3 vector, Func<int, bool> indexPredicate)
        {
            var dstSqr = float.MaxValue;
            var index = 0;
            for (var i = 0; i < MeanPositions.Count; ++i)
            {
                if (!indexPredicate(i)) continue;
                var currDstSqr = (MeanPositions[i] - vector).sqrMagnitude;
                if (currDstSqr < dstSqr)
                {
                    dstSqr = currDstSqr;
                    index = i;
                }
            }

            return (index, dstSqr);
        }

        /// <summary>
        /// Get the closest (index, squared distance) tuple to a given vertex.
        /// </summary>
        /// <param name="vector">The vector to measure against.</param>
        /// <returns>The (index, squared distance) tuple.</returns>
        public (int, float) GetClosestTo(Vector3 vector)
        {
            return GetClosestTo(vector, index => true);
        }

        /// <summary>
        /// Get the best scoring index closest to a vector in a given direction.
        /// </summary>
        /// <param name="vector">The starting point.</param>
        /// <param name="directionPreference">The given direction.</param>
        /// <param name="myIndex">The starting points index, which should not be allowed as a return value.</param>
        /// <param name="bannedTuples">Which tuples of indices are not allowed (myIndex, destinationIndex).</param>
        /// <returns>THe best scoring index.</returns>
        public int GetBestScoringInDirection(Vector3 vector, Vector3 directionPreference, int myIndex,
            HashSet<(int, int)> bannedTuples)
        {
            var sorted = MeanPositions.Select((mean, idx) => ((mean - vector).sqrMagnitude, idx))
                .OrderBy(tuple => tuple.sqrMagnitude).Select(tuple => tuple.idx).ToArray();
            var maximumDot = -1f;
            var index = -1;

            var triedOut = 0;
            for (var i = 0; triedOut < 4 && i < sorted.Length; ++i)
            {
                var meanIndex = sorted[i];
                if (myIndex == meanIndex)
                    continue;
                if (bannedTuples.Contains(MathExtensions.SortTuple(meanIndex, myIndex)))
                    continue;
                var dot = Vector3.Dot((MeanPositions[meanIndex] - vector).normalized, directionPreference);
                if (dot > maximumDot)
                {
                    maximumDot = dot;
                    index = meanIndex;
                }

                ++triedOut;
            }

            return index;
        }
    }
}