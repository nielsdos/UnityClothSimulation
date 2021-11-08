//#define DEBUG_GIZMOS
//#define R_KEY_RESETS_CLOTH

using System.Collections.Generic;
using System.Linq;
using Configuration;
using SoftBody.Collision;
using SoftBody.Cpu;
using SoftBody.Gpu;
using Unity.Collections;
using UnityEngine;

namespace SoftBody
{
    /// <summary>
    /// Base class for every cloth simulation type,
    /// whether it is based on a mesh or a generated mesh at runtime.
    /// </summary>
    public abstract class ClothSimulation : MonoBehaviour
    {
        [Header("Colliders")]
        [SerializeField] private SphereCollider[] sphereColliders;
        [SerializeField] private BoxCollider[] cuboidColliders;
        
        [Header("Configuration of objects used to simulate the cloth")]
        [SerializeField] protected GameObject bonePrefab;
        [Tooltip("Spatial hash grid size in case of CPU simulation")]
        [SerializeField] private float spatialHashingGridSize = 0.1f;
        [SerializeField] private ComputeShader computeShader;
        
        [Header("Cloth simulation parameters")]
        [SerializeField] private bool clothUsesGpuSimulation = true;
        [Tooltip("To allow for stiffer springs, we have to stabilise the simulation by increasing the amount of simulations per delta time frame. Increasing this will decrease performance")]
        [Range(1, 30)]
        [SerializeField] private uint deltaTimeDivisor = 20;
        [SerializeField] private float gravityMultiplier = 0.25f;
        [Tooltip("Integration type to use in case of CPU simulation (does not affect GPU simulation)")]
        [SerializeField] private IntegrationType integrationType = IntegrationType.Verlet;
        [SerializeField] private float elasticSpringConstant = 5400f;
        [SerializeField] private float shearSpringConstant = 3000f;
        [SerializeField] private float bendSpringConstant = 2400f;
        [SerializeField] private float meshBasedElasticSpringConstant = 2400f;
        [SerializeField] private float meshBasedShearSpringConstant = 2400f;
        [SerializeField] private float springInverseMass = 0.125f;
        [Range(10f, 200f)]
        [SerializeField] private float springDamping = 38f;
        [Tooltip("The ratio of the final to initial relative velocity between two objects after collision.")]
        [Range(0f, 1f)]
        [SerializeField] private float restitutionConstant = 0.02f;
        [Range(0f, 1f)]
        [SerializeField] private float frictionConstant = 0.95f;

        protected IClothSpringProcessor SpringProcessor;

        protected Transform[] Bones;
        
        protected virtual void Start()
        {
            var config = new PhysicsWorldConfiguration
            {
                GravityMultiplier = gravityMultiplier,
                FrictionConstant = frictionConstant,
                RestitutionConstant = restitutionConstant,
                SpringDamping = springDamping,
                SpringInverseMass = springInverseMass,
                MeshBasedElasticSpringConstant = meshBasedElasticSpringConstant,
                MeshBasedShearSpringConstant = meshBasedShearSpringConstant,
                BendSpringConstant = bendSpringConstant,
                ShearSpringConstant = shearSpringConstant,
                ElasticSpringConstant = elasticSpringConstant,
                IntegrationType = integrationType,
                DeltaTimeDivisor = deltaTimeDivisor
            };

            if (clothUsesGpuSimulation)
                SpringProcessor = new GpuClothSpringProcessor(config, Instantiate(computeShader));
            else
                SpringProcessor = new CpuClothSpringProcessor(config, spatialHashingGridSize);
        }

        /// <summary>
        /// Initializes the cloth simulation.
        /// </summary>
        /// <param name="mesh">The cloth mesh.</param>
        /// <param name="bones">The transforms of the bones.</param>
        protected void Initialize(Mesh mesh, Transform[] bones)
        {
            SpringProcessor.FinishInitialization();

            Bones = bones;

            mesh.bindposes = CalculateBindPosesFromTransforms(bones);

            var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.bones = bones;
            var bounds = mesh.bounds;
            var middleBone = bones[bones.Length / 2];
            bounds.center += transform.position - middleBone.position;
            skinnedMeshRenderer.localBounds = bounds;
            skinnedMeshRenderer.rootBone = middleBone;
            skinnedMeshRenderer.sharedMesh = mesh;
        }

        /// <summary>
        /// Enumerates spring nodes nearby a given sphere.
        /// </summary>
        /// <param name="centroid">The sphere centroid.</param>
        /// <param name="radius">The sphere radius.</param>
        /// <returns>An enumerable for the nearby spring nodes.</returns>
        public IEnumerable<ISpringNode> EnumerateNearbySphere(Vector3 centroid, float radius)
        {
            return SpringProcessor.EnumerateNearbySphere(centroid, radius);
        }

        /// <summary>
        /// Calculates the bind pose matrices from the bone transforms, which will be the inverse transformed positions.
        /// </summary>
        /// <param name="boneTransforms">An iterable over the bone transforms.</param>
        /// <returns>The bind pose matrices.</returns>
        private Matrix4x4[] CalculateBindPosesFromTransforms(IEnumerable<Transform> boneTransforms)
        {
            // Bone pose wants to have the inverse transformed position.
            var localToWorldMatrix = transform.localToWorldMatrix;
            return (from boneTransform in boneTransforms select boneTransform.worldToLocalMatrix * localToWorldMatrix)
                .ToArray();
        }

        private void FixedUpdate()
        {
            var sphereCollisionProxies = new NativeArray<ImmovableSphereCollisionAdapter>(sphereColliders.Length,
                Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var cuboidCollisionProxies = new NativeArray<ImmovableCuboidCollisionAdapter>(cuboidColliders.Length,
                Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (var i = 0; i < sphereColliders.Length; ++i)
                sphereCollisionProxies[i] = new ImmovableSphereCollisionAdapter(sphereColliders[i]);

            for (var i = 0; i < cuboidColliders.Length; ++i)
                cuboidCollisionProxies[i] = new ImmovableCuboidCollisionAdapter(cuboidColliders[i]);

            // Since we need an order inside FixedUpdate we can't just use the unordered execution order of 
            // FixedUpdate on different game objects.
            var deltaTime = Time.fixedDeltaTime;
            SpringProcessor.FixedUpdate(deltaTime, sphereCollisionProxies, cuboidCollisionProxies);

#if R_KEY_RESETS_CLOTH
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetToInitialState();
            }
#endif

            cuboidCollisionProxies.Dispose();
            sphereCollisionProxies.Dispose();
        }

#if DEBUG_GIZMOS
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            SpringProcessor?.OnDrawGizmos();
        }
#endif

        private void OnDestroy()
        {
            SpringProcessor.OnDestroy();
        }

        /// <summary>
        /// Gets the bones of the cloth.
        /// </summary>
        /// <returns>The bones array.</returns>
        public Transform[] GetBones()
        {
            return Bones;
        }

        /// <summary>
        /// Resets the cloth to its initial start position
        /// </summary>
        public void ResetToInitialState()
        {
            SpringProcessor.ResetToInitialState();
        }
    }
}