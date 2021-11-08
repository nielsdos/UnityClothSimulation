using System.Collections.Generic;
using Configuration;
using SoftBody.Collision;
using SoftBody.Cpu.Integration.Factory;
using SoftBody.DataStructures;
using Unity.Collections;
using UnityEngine;

namespace SoftBody.Cpu
{
    /// <summary>
    /// This class is responsible for setting up, and managing the cpu implementation 
    /// for the spring processing, including force calculation, integration and collisions.
    /// </summary>
    public sealed class CpuClothSpringProcessor : IClothSpringProcessor
    {
        private readonly List<SpringNode> _springNodes;
        private readonly List<SpringDamper> _springDampers;
        private readonly SpatialHasher<SpringNode> _spatialHasher;
        private readonly PhysicsWorldConfiguration _physicsWorldConfiguration;
        private readonly IIntegratorFactory _integratorFactory;

        /// <summary>
        /// Creates a ne Cpu based spring processor.
        /// </summary>
        /// <param name="physicsWorldConfiguration">The physics configuration.</param>
        /// <param name="spatialHashGridSize">The size of the grid in the spatial hasher.</param>
        public CpuClothSpringProcessor(PhysicsWorldConfiguration physicsWorldConfiguration, float spatialHashGridSize)
        {
            _springNodes = new List<SpringNode>();
            _springDampers = new List<SpringDamper>();
            _physicsWorldConfiguration = physicsWorldConfiguration;
            _integratorFactory = IntegratorFactoryProvider.GetFactoryFor(_physicsWorldConfiguration.IntegrationType);
            _spatialHasher = new SpatialHasher<SpringNode>(spatialHashGridSize);
        }

        /// <inheritdoc cref="IClothSpringProcessor.SpringNodeFor"/>
        public ISpringNode SpringNodeFor(int index)
        {
            return _springNodes[index];
        }

        /// <inheritdoc cref="IClothSpringProcessor.AddSpringNode"/>
        public int AddSpringNode(Transform correspondingBoneTransform, float radius)
        {
            var springNode = new SpringNode(correspondingBoneTransform, _integratorFactory.Create(), radius,
                _physicsWorldConfiguration, _springNodes.Count);
            _springNodes.Add(springNode);
            _spatialHasher.Insert(springNode);
            return _springNodes.Count - 1;
        }

        /// <inheritdoc cref="IClothSpringProcessor.AddSpringDamper"/>
        public void AddSpringDamper(SpringDamperType type, float desiredDistance, int firstNodeIndex,
            int secondNodeIndex)
        {
            var springConstant = _physicsWorldConfiguration.SpringConstantForType(type);
            var springDamping = _physicsWorldConfiguration.SpringDamping;
            _springDampers.Add(new SpringDamper(_springNodes[firstNodeIndex], _springNodes[secondNodeIndex],
                desiredDistance, springConstant, springDamping));
        }

        /// <inheritdoc cref="IClothSpringProcessor.FixedUpdate"/>
        public void FixedUpdate(float deltaTime, NativeArray<ImmovableSphereCollisionAdapter> sphereColliders,
            NativeArray<ImmovableCuboidCollisionAdapter> cuboidColliders)
        {
            deltaTime /= _physicsWorldConfiguration.DeltaTimeDivisor;

            for (var t = 0; t < _physicsWorldConfiguration.DeltaTimeDivisor; ++t)
            {
                foreach (var spring in _springDampers) spring.ApplySpringForce();

                foreach (var node in _springNodes) node.ApplyForce(deltaTime);
            }

            foreach (var myNode in _springNodes)
            {
                _spatialHasher.Update(myNode);

                foreach (var otherNode in _spatialHasher.EnumerateNear(myNode))
                    if (!ReferenceEquals(myNode, otherNode))
                        myNode.RespondToPossibleCollision(otherNode);

                // Spatial hasher will check whether an update is really necessary.
                // We can't update in the loop because the collection would become invalid.
                _spatialHasher.Update(myNode);

                // Fixed objects in scene.
                for (var i = 0; i < sphereColliders.Length; ++i)
                    myNode.RespondToPossibleCollision(sphereColliders[i]);
                for (var i = 0; i < cuboidColliders.Length; ++i)
                    myNode.RespondToPossibleCollision(cuboidColliders[i]);
            }
        }

        /// <inheritdoc cref="IClothSpringProcessor.EnumerateNearbySphere"/>
        public IEnumerable<ISpringNode> EnumerateNearbySphere(Vector3 centroid, float radius)
        {
            var sqrRadius = radius * radius;
            foreach (var node in _spatialHasher.EnumerateNear(new SpatialHasher<SpringNode>.Query
            {
                Centroid = centroid,
                Radius = radius
            }))
            {
                var difference = centroid - node.Centroid;
                if (difference.sqrMagnitude <= sqrRadius) yield return node;
            }
        }

        /// <inheritdoc cref="IClothSpringProcessor.SyncPositionUpdatesImmediately"/>
        public void SyncPositionUpdatesImmediately()
        {
            foreach (var myNode in _springNodes) _spatialHasher.Update(myNode);
        }

        /// <inheritdoc cref="IClothSpringProcessor.OnDrawGizmos"/>
        public void OnDrawGizmos()
        {
            foreach (var springDamper in _springDampers)
            {
                var (f, s) = springDamper.GetAttachedNodes();
                Gizmos.DrawLine(f.Position, s.Position);
            }
        }

        /// <inheritdoc cref="IClothSpringProcessor.FinishInitialization"/>
        public void FinishInitialization()
        {
        }

        /// <inheritdoc cref="IClothSpringProcessor.OnDestroy"/>
        public void OnDestroy()
        {
        }

        /// <inheritdoc cref="IClothSpringProcessor.ResetToInitialState"/>
        public void ResetToInitialState()
        {
            foreach (var springNode in _springNodes) springNode.ResetToInitialState();
        }
    }
}