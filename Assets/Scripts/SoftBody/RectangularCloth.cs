using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoftBody
{
    public sealed partial class RectangularCloth : ClothSimulation
    {
        [SerializeField] private float width = 10f;
        [SerializeField] private float height = 10f;
        [SerializeField] private Vector2 clothTextureTiling = new Vector2(1f, 1f);

        [Tooltip(
            "This is the amount of divisions in a single axis that's made in the mesh. Increasing this will increase the amount of particles in both directions.")]
        [SerializeField]
        private int amountOfDivisionsInMesh = 10;

        protected override void Start()
        {
            base.Start();

            var mesh = new Mesh();

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var boneWeights = new List<BoneWeight>();
            var bones = new List<Transform>();

            var stepSize = 1f / amountOfDivisionsInMesh;
            var scale2 = new Vector2(width, height) * stepSize;
            var scale3 = new Vector3(scale2.x, 0f, scale2.y);

            for (var x = 0; x < amountOfDivisionsInMesh; ++x)
            for (var z = 0; z < amountOfDivisionsInMesh; ++z)
            {
                var triangleIndex = vertices.Count;

                // Square, do this twice because otherwise the shared normal calculations would be incorrect.
                for (var i = 0; i < 2; ++i)
                {
                    vertices.Add(Vector3.Scale(new Vector3(x, 0, z), scale3));
                    vertices.Add(Vector3.Scale(new Vector3(x, 0, z + 1), scale3));
                    vertices.Add(Vector3.Scale(new Vector3(x + 1, 0, z), scale3));
                    vertices.Add(Vector3.Scale(new Vector3(x + 1, 0, z + 1), scale3));

                    // UV (texturing)
                    uvs.Add(Vector2.Scale(new Vector2(x, z) * clothTextureTiling, scale2));
                    uvs.Add(Vector2.Scale(new Vector2(x, z + 1) * clothTextureTiling, scale2));
                    uvs.Add(Vector2.Scale(new Vector2(x + 1, z) * clothTextureTiling, scale2));
                    uvs.Add(Vector2.Scale(new Vector2(x + 1, z + 1) * clothTextureTiling, scale2));
                }

                // First triangle of quad.
                triangles.Add(triangleIndex);
                triangles.Add(triangleIndex + 1);
                triangles.Add(triangleIndex + 2);

                // Second triangle of quad.
                triangles.Add(triangleIndex + 1);
                triangles.Add(triangleIndex + 3);
                triangles.Add(triangleIndex + 2);

                // First upside-down triangle of quad.
                triangles.Add(triangleIndex + 4);
                triangles.Add(triangleIndex + 6);
                triangles.Add(triangleIndex + 5);

                // Second upside-down triangle of quad.
                triangles.Add(triangleIndex + 5);
                triangles.Add(triangleIndex + 6);
                triangles.Add(triangleIndex + 7);
            }

            // Create bones
            const float collisionMarginPercentage = 0.95f;
            var radius = stepSize / 2f * collisionMarginPercentage * Mathf.Min(width, height);
            for (var x = 0; x <= amountOfDivisionsInMesh; ++x)
            for (var z = 0; z <= amountOfDivisionsInMesh; ++z)
            {
                var boneObject = Instantiate(bonePrefab, Vector3.zero, Quaternion.identity, transform);
                var position = Vector3.Scale(new Vector3(x, 0f, z), scale3);
                boneObject.transform.localPosition = position;
                boneObject.name = $"Bone ({x}, {z})";
                bones.Add(boneObject.transform);
                SpringProcessor.AddSpringNode(boneObject.transform, radius);
            }

            var xDesiredDistance = stepSize * width;
            var zDesiredDistance = stepSize * height;
            var xzDesiredDistance =
                Mathf.Sqrt(xDesiredDistance * xDesiredDistance + zDesiredDistance * zDesiredDistance);

            for (var x = 0; x <= amountOfDivisionsInMesh; ++x)
            for (var z = 0; z <= amountOfDivisionsInMesh; ++z)
            {
                var myNodeIndex = CoordToIndex(x, z);

                void TryAddSpringDamper(SpringDamperType type, int xx, int zz, float desiredDistance)
                {
                    if (xx >= 0 && zz >= 0 && xx <= amountOfDivisionsInMesh && zz <= amountOfDivisionsInMesh)
                    {
                        var otherNodeIndex = CoordToIndex(xx, zz);
                        SpringProcessor.AddSpringDamper(type, desiredDistance, myNodeIndex, otherNodeIndex);
                    }
                }

                // Elastic springs
                TryAddSpringDamper(SpringDamperType.Elastic, x + 1, z, xDesiredDistance);
                TryAddSpringDamper(SpringDamperType.Elastic, x, z + 1, zDesiredDistance);

                // Shear springs
                TryAddSpringDamper(SpringDamperType.Shear, x + 1, z + 1, xzDesiredDistance);
                TryAddSpringDamper(SpringDamperType.Shear, x + 1, z - 1, xzDesiredDistance);

                // Bend springs
                TryAddSpringDamper(SpringDamperType.Bend, x + 2, z, xDesiredDistance * 2f);
                TryAddSpringDamper(SpringDamperType.Bend, x, z + 2, zDesiredDistance * 2f);
            }

            for (var x = 0; x < amountOfDivisionsInMesh; ++x)
            for (var z = 0; z < amountOfDivisionsInMesh; ++z)
            {
                (int, float) GetBoneWeightTuple(int xx, int zz)
                {
                    if (xx <= amountOfDivisionsInMesh && zz <= amountOfDivisionsInMesh)
                        return (CoordToIndex(xx, zz), 1f / 3f);

                    return (0, 0.0f);
                }

                BoneWeight CalculateBoneWeight(int xx, int zz)
                {
                    var mainBone = CoordToIndex(xx, zz);
                    var boneWeight = new BoneWeight
                    {
                        boneIndex0 = mainBone,
                        weight0 = 1.0f
                    };

                    int index;
                    float weight;

                    (index, weight) = GetBoneWeightTuple(xx + 1, zz);
                    boneWeight.boneIndex1 = index;
                    boneWeight.weight1 = weight;

                    (index, weight) = GetBoneWeightTuple(xx, zz + 1);
                    boneWeight.boneIndex2 = index;
                    boneWeight.weight2 = weight;

                    (index, weight) = GetBoneWeightTuple(xx + 1, zz + 1);
                    boneWeight.boneIndex3 = index;
                    boneWeight.weight3 = weight;

                    // We don't know how many we actually added, so rescale now such that the sum of the weights is 1.
                    var total = boneWeight.weight0 + boneWeight.weight1 + boneWeight.weight2 +
                                boneWeight.weight3;
                    boneWeight.weight0 /= total;
                    boneWeight.weight1 /= total;
                    boneWeight.weight2 /= total;
                    boneWeight.weight3 /= total;

                    return boneWeight;
                }

                for (var i = 0; i < 2; ++i)
                {
                    boneWeights.Add(CalculateBoneWeight(x, z));
                    boneWeights.Add(CalculateBoneWeight(x, z + 1));
                    boneWeights.Add(CalculateBoneWeight(x + 1, z));
                    boneWeights.Add(CalculateBoneWeight(x + 1, z + 1));
                }
            }

            // By default, Unity uses UInt16 format for the indices of the vertices, such that it uses
            // less memory and bandwidth than the UInt32 format. However, that would limit us to 65535 vertices.
            // Set the format to UInt32 if necessary, but only if there's no other choice, such that we don't lose
            // performance in the typical case.
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;

            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.boneWeights = boneWeights.ToArray();

            // Need to recalculate both to make sure lighting and bounding box will be correct.
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Initialize(mesh, bones.ToArray());
        }

        /// <summary>
        /// Rotate the cloth 90 degrees
        /// </summary>
        public void Rotate()
        {
            for (var x = 0; x <= amountOfDivisionsInMesh; x++)
            for (var z = 0; z <= amountOfDivisionsInMesh; z++)
            {
                var springNode = SpringProcessor.SpringNodeFor(CoordToIndex(x, z));
                var springNodePosition = springNode.Position;
                var temp = springNodePosition.x;
                springNodePosition.x = springNodePosition.z - 1.6f;
                springNodePosition.z = -temp + 3.1f;
                springNode.SnapTo(springNodePosition, false);
            }

            SpringProcessor.SyncPositionUpdatesImmediately();
        }

        /// <summary>
        /// Map a coordinate to a cloth index
        /// </summary>
        /// <param name="xx">The x coordinate</param>
        /// <param name="zz">The z coordinate</param>
        /// <returns>Index for the array of cloth nodes</returns>
        private int CoordToIndex(int xx, int zz)
        {
            Debug.Assert(xx <= amountOfDivisionsInMesh);
            Debug.Assert(zz <= amountOfDivisionsInMesh);
            return xx * (amountOfDivisionsInMesh + 1) + zz;
        }

        /// <summary>
        /// Get the amount of points of the cloth on the x axis
        /// </summary>
        /// <returns>Number of points on x axis</returns>
        public int GetPointsXAxis()
        {
            return amountOfDivisionsInMesh + 1;
        }

        /// <summary>
        /// Get the amount of points of the cloth on the z axis
        /// </summary>
        /// <returns>Number of points on z axis</returns>
        public int GetPointsZAxis()
        {
            return amountOfDivisionsInMesh + 1;
        }
    }
}