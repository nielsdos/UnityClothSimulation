using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utility;

namespace SoftBody
{
    /// <summary>
    /// Applies cloth simulation to a model instead of creating the mesh at runtime.
    /// </summary>
    public sealed class ModelBasedCloth : ClothSimulation
    {
        [SerializeField] private float minimumGroupDistance = 1f;
        [SerializeField] private int maxAmountOfParticles = 250;

        [Tooltip(
            "Cloths like shirts are shorter in one axis than another. We need to correct this for grouping the vertices such that we don't group two different parts together.")]
        [SerializeField]
        private Vector3 axisScaling = Vector3.one;

        protected override void Start()
        {
            base.Start();

            var meshFilter = GetComponent<MeshFilter>();
            var mesh = meshFilter.sharedMesh;

            // For some reason, caching these here gives a performance improvement.
            // Probably because engine native code round trips that are avoided.
            var vertices = new List<Vector3>();
            mesh.GetVertices(vertices);
            var localScale = transform.localScale;
            var localScaledVertices = vertices.Select(vertex => Vector3.Scale(vertex, localScale)).ToArray();

            var meanInfo =
                new MeanVertexInfoTracker(mesh, localScaledVertices, axisScaling, minimumGroupDistance,
                    maxAmountOfParticles);

            var bones = new List<Transform>();
            var springRadii = new List<float>();

            var minimalRadiusEverSeen = float.MaxValue;
            foreach (var meanPosition in meanInfo.MeanPositions)
            {
                var boneId = bones.Count;
                var boneObject = Instantiate(bonePrefab, Vector3.zero, Quaternion.identity, transform);
                boneObject.transform.localPosition = meanPosition;
                boneObject.name = $"Bone {boneId}";

                // Find the closest other node, then the distance will be divided over the two nodes,
                // such that the half distance can become our radius.
                var scale = Mathf.Sqrt(meanInfo.GetClosestTo(meanPosition, index => index != boneId).Item2) / 2f;
                minimalRadiusEverSeen = Mathf.Min(minimalRadiusEverSeen, scale);

                bones.Add(boneObject.transform);
                springRadii.Add(scale);

                SpringProcessor.AddSpringNode(boneObject.transform, scale);
            }

            var createdSpringDamperTuples = new HashSet<(int, int)>();

            void CreateSpringDamper(SpringDamperType type, int firstIdx, int secondIdx)
            {
                if (secondIdx == -1) return;
                if (firstIdx == secondIdx) return;
                createdSpringDamperTuples.Add(MathExtensions.SortTuple(firstIdx, secondIdx));
                var firstStartLocation = bones[firstIdx].position;
                var secondStartLocation = bones[secondIdx].position;
                SpringProcessor.AddSpringDamper(type, Vector3.Distance(firstStartLocation, secondStartLocation),
                    firstIdx, secondIdx);
            }

            // Heuristic
            var directionStretching = minimalRadiusEverSeen / 2.5f;
            for (var nodeIndex = 0; nodeIndex < meanInfo.MeanPositions.Count; ++nodeIndex)
            {
                var meanPosition = meanInfo.MeanPositions[nodeIndex];
                var meanTangent = meanInfo.MeanTangents[nodeIndex];
                var meanNormal = meanInfo.MeanNormals[nodeIndex];

                var perpTangent = Vector3.Cross(meanTangent, meanNormal);

                // Note possible improvement: prevent the thing of creating connections "over" other nodes?

                // Elastic springs
                CreateSpringDamper(SpringDamperType.MeshElastic,
                    meanInfo.GetBestScoringInDirection(meanPosition + meanTangent * directionStretching,
                        meanTangent.normalized,
                        nodeIndex, createdSpringDamperTuples),
                    nodeIndex);
                CreateSpringDamper(SpringDamperType.MeshElastic,
                    meanInfo.GetBestScoringInDirection(meanPosition + perpTangent * directionStretching,
                        perpTangent.normalized,
                        nodeIndex, createdSpringDamperTuples),
                    nodeIndex);

                var mainDiagonal = (meanTangent + perpTangent).normalized;
                var notMainDiagonal = (meanTangent - perpTangent).normalized;

                // Shear springs
                CreateSpringDamper(SpringDamperType.MeshShear,
                    meanInfo.GetBestScoringInDirection(meanPosition + mainDiagonal * directionStretching,
                        mainDiagonal,
                        nodeIndex, createdSpringDamperTuples), nodeIndex);
                CreateSpringDamper(SpringDamperType.MeshShear,
                    meanInfo.GetBestScoringInDirection(meanPosition + notMainDiagonal * directionStretching,
                        notMainDiagonal,
                        nodeIndex, createdSpringDamperTuples), nodeIndex);
            }

            var bonesArray = bones.ToArray();

            mesh.boneWeights = CalculateBoneWeights(meanInfo, bonesArray, springRadii.ToArray(), localScaledVertices);

            // Convert to a skinned mesh renderer and initialize the cloth simulation.
            var meshRenderer = GetComponent<MeshRenderer>();
            var materials = meshRenderer.materials;
            Destroy(meshRenderer);
            Destroy(meshFilter);
            var skinnedMeshRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.materials = materials;
            skinnedMeshRenderer.updateWhenOffscreen = true;
            Initialize(mesh, bonesArray);
        }

        /// <summary>
        /// Calculate the bone weights for the given vertices and mean info.
        /// </summary>
        /// <param name="meanInfo">Information about the mean points, used for falling back when no appropriate weight can be found.</param>
        /// <param name="bones">The bones corresponding to the spring nodes.</param>
        /// <param name="radii">The radii corresponding to the spring nodes.</param>
        /// <param name="localScaledVertices">The vertices of the mesh scaled with the local scale.</param>
        /// <returns>The bone weights.</returns>
        private BoneWeight[] CalculateBoneWeights(MeanVertexInfoTracker meanInfo, Transform[] bones, float[] radii,
            Vector3[] localScaledVertices)
        {
            var boneWeights = new BoneWeight[localScaledVertices.Length];

            var mapVertexIdToWeights = new Dictionary<int, List<BoneWeight1>>();

            for (var springId = 0; springId < bones.Length; ++springId)
            {
                // Sphere of influence.
                // Factor 2 undoes the half-scaling done earlier.
                var sphereRadius = radii[springId] * 2f;
                var sqrSphereRadius = sphereRadius * sphereRadius;
                var sphereCentroid = bones[springId].localPosition;

                // Find all influences of this sphere.
                for (var vertexId = 0; vertexId < localScaledVertices.Length; ++vertexId)
                {
                    var difference = localScaledVertices[vertexId] - sphereCentroid;
                    var sqrDst = difference.sqrMagnitude;
                    if (sqrDst < sqrSphereRadius)
                    {
                        if (!mapVertexIdToWeights.TryGetValue(vertexId, out var weights))
                            mapVertexIdToWeights.Add(vertexId, weights = new List<BoneWeight1>());
                        var weight = (sphereRadius - Mathf.Sqrt(sqrDst)) / sphereRadius;
                        weights.Add(new BoneWeight1
                        {
                            boneIndex = springId,
                            weight = weight
                        });
                    }
                }
            }

            for (var vertexId = 0; vertexId < localScaledVertices.Length; ++vertexId)
            {
                if (!mapVertexIdToWeights.TryGetValue(vertexId, out var weights))
                {
                    // Fallback.
                    boneWeights[vertexId] = new BoneWeight
                    {
                        boneIndex0 = meanInfo.GetClosestTo(localScaledVertices[vertexId]).Item1,
                        weight0 = 1f
                    };
                    continue;
                }

                weights.Sort((a, b) => b.weight.CompareTo(a.weight));
                boneWeights[vertexId] = new BoneWeight
                {
                    boneIndex0 = weights[0].boneIndex,
                    weight0 = weights[0].weight,
                    boneIndex1 = weights.Count <= 1 ? 0 : weights[1].boneIndex,
                    weight1 = weights.Count <= 1 ? 0 : weights[1].weight,
                    boneIndex2 = weights.Count <= 2 ? 0 : weights[2].boneIndex,
                    weight2 = weights.Count <= 2 ? 0 : weights[2].weight,
                    boneIndex3 = weights.Count <= 3 ? 0 : weights[3].boneIndex,
                    weight3 = weights.Count <= 3 ? 0 : weights[3].weight
                };

                var totalWeight = boneWeights[vertexId].weight0 + boneWeights[vertexId].weight1 +
                                  boneWeights[vertexId].weight2 + boneWeights[vertexId].weight3;
                boneWeights[vertexId].weight0 /= totalWeight;
                boneWeights[vertexId].weight1 /= totalWeight;
                boneWeights[vertexId].weight2 /= totalWeight;
                boneWeights[vertexId].weight3 /= totalWeight;
            }

            return boneWeights;
        }
    }
}