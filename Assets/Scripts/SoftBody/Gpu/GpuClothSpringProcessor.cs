using System.Collections.Generic;
using System.Runtime.InteropServices;
using Configuration;
using SoftBody.Collision;
using Unity.Collections;
using UnityEngine;

namespace SoftBody.Gpu
{
    /// <summary>
    /// This class is responsible for setting up, and managing the compute shader responsible
    /// for the spring processing, including force calculation, integration and collisions.
    /// </summary>
    public sealed class GpuClothSpringProcessor : IClothSpringProcessor
    {
        /// <summary>
        /// Proxies the calls to the spring node.
        /// This solves the issue of passing copies of a value type inheriting an interface.
        /// This is a nested struct because it should have access to the private parts of its parent.
        /// <seealso cref="ISpringNode"/>
        /// </summary>
        private readonly struct GpuSpringNodeProxy : ISpringNode
        {
            private readonly GpuClothSpringProcessor _gpuClothSpringProcessor;
            public Vector3 Position { get; }
            public int ParticleIndex { get; }

            /// <summary>
            /// Creates a new proxy for the Gpu based spring node.
            /// </summary>
            /// <param name="gpuClothSpringProcessor">The processor holding all the spring node data.</param>
            /// <param name="springNodeIndex">The index of the spring node this struct is proxying.</param>
            internal GpuSpringNodeProxy(GpuClothSpringProcessor gpuClothSpringProcessor, int springNodeIndex)
            {
                _gpuClothSpringProcessor = gpuClothSpringProcessor;
                ParticleIndex = springNodeIndex;
                Position = gpuClothSpringProcessor._springNodes[springNodeIndex].Position;
            }

            public void SnapTo(Vector3 newPosition, bool updateVelocity)
            {
                _gpuClothSpringProcessor._springNodes[ParticleIndex].SnapTo(newPosition, updateVelocity);
            }
        }

        private readonly ComputeShader _computeShader;
        private ComputeBuffer _springNodeComputeBuffer;
        private readonly ComputeBuffer _sentinelSphereColliderComputeBuffer, _sentinelCuboidColliderComputeBuffer;
        private ComputeBuffer _springNodeOutputComputeBuffer;
        private int _forceKernel, _collisionKernel;
        private readonly List<SpringDamper> _springDampers;
        private SpringNode[] _springNodes;
        private SpringNodeOutput[] _springNodeOutputs;
        private Vector3[] _initialPositions;
        private List<SpringNode> _tempSpringNodeList;
        private readonly List<Transform> _springNodeBones;
        private ComputeBuffer _springDampersBuffer;
        private readonly PhysicsWorldConfiguration _physicsWorldConfiguration;

        /// <summary>
        /// We need to be able to provide a contiguous area of spring damper data.
        /// But spring dampers may be added in any order.
        /// So we group them here so we can nicely concatenate them later in a contiguous manner.
        /// </summary>
        private Dictionary<int, List<SpringDamper>> _temporarySpringDamperMapper;

        private const int NumThreads = 32;

        /// <summary>
        /// Creates a new Gpu based spring processor.
        /// </summary>
        /// <param name="physicsWorldConfiguration">The physics configuration.</param>
        /// <param name="computeShader">The compute shader responsible for the spring simulation.</param>
        public GpuClothSpringProcessor(PhysicsWorldConfiguration physicsWorldConfiguration, ComputeShader computeShader)
        {
            _physicsWorldConfiguration = physicsWorldConfiguration;
            _computeShader = computeShader;
            _springDampers = new List<SpringDamper>();
            _tempSpringNodeList = new List<SpringNode>();
            _springNodeBones = new List<Transform>();
            _temporarySpringDamperMapper = new Dictionary<int, List<SpringDamper>>();

            _sentinelSphereColliderComputeBuffer =
                new ComputeBuffer(1, Marshal.SizeOf<ImmovableSphereCollisionAdapter>());
            _sentinelSphereColliderComputeBuffer.SetData(new[] {new ImmovableSphereCollisionAdapter(new Vector3(-99999f, -99999f, -99999f), 0f)});
            
            _sentinelCuboidColliderComputeBuffer =
                new ComputeBuffer(1, Marshal.SizeOf<ImmovableCuboidCollisionAdapter>());
            _sentinelCuboidColliderComputeBuffer.SetData(new[] {new ImmovableCuboidCollisionAdapter(new Vector3(-99999f, -99999f, -99999f), Vector3.one)});
        }

        /// <summary>
        /// Converts the information about which spring damper is connected between which nodes,
        /// to a format we use on the Gpu.
        /// </summary>
        private void TransformDataIntoCorrectGpuFormat()
        {
            _springNodes = _tempSpringNodeList.ToArray();
            _initialPositions = new Vector3[_tempSpringNodeList.Count];

            uint currentStart = 0;
            for (var i = 0; i < _springNodes.Length; ++i)
            {
                var list = _temporarySpringDamperMapper[i];
                _springNodes[i].SetSlice(new Slice(currentStart, currentStart + (uint) list.Count));
                // Place spring dampers sequentially.
                _springDampers.AddRange(list);
                currentStart += (uint) list.Count;
                // Store initial position so we can restore later when episode ends.
                _initialPositions[i] = _springNodes[i].Position;
            }

            // No longer needed. Free its memory.
            _temporarySpringDamperMapper = null;
            _tempSpringNodeList = null;
        }

        /// <summary>
        /// Finds the kernels we use and initializes the variables and buffers of the compute shader.
        /// </summary>
        private void SetupComputeShaderVariablesAndBuffers()
        {
            _forceKernel = _computeShader.FindKernel("CalculateAndApplySpringForce");
            _collisionKernel = _computeShader.FindKernel("ResolveCollisions");

            _computeShader.SetFloat("SpringDamping", _physicsWorldConfiguration.SpringDamping);
            var gravity = _physicsWorldConfiguration.Gravity;
            _computeShader.SetFloats("Gravity", gravity.x, gravity.y, gravity.z);
            _computeShader.SetFloat("InverseMass", _physicsWorldConfiguration.SpringInverseMass);
            _computeShader.SetFloat("RestitutionConstant", _physicsWorldConfiguration.RestitutionConstant);
            _computeShader.SetFloat("FrictionConstant", _physicsWorldConfiguration.FrictionConstant);
            _computeShader.SetInt("SpringNodeCount", _springNodes.Length);
            _computeShader.SetInt("DeltaTimeDivisor", (int) _physicsWorldConfiguration.DeltaTimeDivisor);

            _springDampersBuffer = new ComputeBuffer(_springDampers.Count, Marshal.SizeOf<SpringDamper>());
            _springDampersBuffer.SetData(_springDampers);
            _computeShader.SetBuffer(_forceKernel, "SpringDampers", _springDampersBuffer);
            _computeShader.SetBuffer(_collisionKernel, "SpringDampers", _springDampersBuffer);

            _springNodeComputeBuffer = new ComputeBuffer(_springNodes.Length, Marshal.SizeOf<SpringNode>());
            _computeShader.SetBuffer(_forceKernel, "SpringNodesInput", _springNodeComputeBuffer);
            _computeShader.SetBuffer(_collisionKernel, "SpringNodesInput", _springNodeComputeBuffer);

            _springNodeOutputComputeBuffer =
                new ComputeBuffer(_springNodes.Length, Marshal.SizeOf<SpringNodeOutput>());
            _computeShader.SetBuffer(_forceKernel, "SpringNodesOutput", _springNodeOutputComputeBuffer);
            _computeShader.SetBuffer(_collisionKernel, "SpringNodesOutput", _springNodeOutputComputeBuffer);

            _springNodeOutputs = new SpringNodeOutput[_springNodes.Length];
            _springNodeOutputComputeBuffer.SetData(_springNodeOutputs);
        }

        /// <inheritdoc cref="IClothSpringProcessor.FinishInitialization"/>
        public void FinishInitialization()
        {
            TransformDataIntoCorrectGpuFormat();
            SetupComputeShaderVariablesAndBuffers();
        }

        /// <inheritdoc cref="IClothSpringProcessor.OnDestroy"/>
        public void OnDestroy()
        {
            _springNodeOutputComputeBuffer.Dispose();
            _sentinelSphereColliderComputeBuffer.Dispose();
            _sentinelCuboidColliderComputeBuffer.Dispose();
            _springDampersBuffer.Dispose();
            _springNodeComputeBuffer.Dispose();
        }

        /// <inheritdoc cref="IClothSpringProcessor.SpringNodeFor"/>
        public ISpringNode SpringNodeFor(int index)
        {
            return new GpuSpringNodeProxy(this, index);
        }

        /// <inheritdoc cref="IClothSpringProcessor.AddSpringNode"/>
        public int AddSpringNode(Transform correspondingBoneTransform, float radius)
        {
            _tempSpringNodeList.Add(new SpringNode(correspondingBoneTransform.transform.position, radius));
            _springNodeBones.Add(correspondingBoneTransform);
            return _tempSpringNodeList.Count - 1;
        }

        /// <inheritdoc cref="IClothSpringProcessor.AddSpringDamper"/>
        public void AddSpringDamper(SpringDamperType type, float desiredDistance, int firstNodeIndex,
            int secondNodeIndex)
        {
            List<SpringDamper> GetOrCreateList(int index)
            {
                if (!_temporarySpringDamperMapper.TryGetValue(index, out var list))
                    _temporarySpringDamperMapper.Add(index, list = new List<SpringDamper>());
                return list;
            }

            var firstNodeSpringDampers = GetOrCreateList(firstNodeIndex);
            var secondNodeSpringDampers = GetOrCreateList(secondNodeIndex);

            var springConstant = _physicsWorldConfiguration.SpringConstantForType(type);

            firstNodeSpringDampers.Add(new SpringDamper((uint) secondNodeIndex, desiredDistance,
                springConstant));
            secondNodeSpringDampers.Add(new SpringDamper((uint) firstNodeIndex, desiredDistance,
                springConstant));
        }

        /// <inheritdoc cref="IClothSpringProcessor.FixedUpdate"/>
        public void FixedUpdate(float deltaTime, NativeArray<ImmovableSphereCollisionAdapter> sphereColliders,
            NativeArray<ImmovableCuboidCollisionAdapter> cuboidColliders)
        {
            ComputeBuffer sphereBuffer = null, cuboidBuffer = null;

            if (cuboidColliders.Length > 0)
            {
                cuboidBuffer =
                    new ComputeBuffer(cuboidColliders.Length, Marshal.SizeOf<ImmovableCuboidCollisionAdapter>());
                cuboidBuffer.SetData(cuboidColliders);
                _computeShader.SetBuffer(_collisionKernel, "Cuboids", cuboidBuffer);
                _computeShader.SetInt("CuboidCount", cuboidColliders.Length);
            }
            else
            {
                _computeShader.SetBuffer(_collisionKernel, "Cuboids", _sentinelCuboidColliderComputeBuffer);
                _computeShader.SetInt("CuboidCount", 0);
            }

            if (sphereColliders.Length > 0)
            {
                sphereBuffer =
                    new ComputeBuffer(sphereColliders.Length, Marshal.SizeOf<ImmovableSphereCollisionAdapter>());
                sphereBuffer.SetData(sphereColliders);
                _computeShader.SetBuffer(_collisionKernel, "Spheres", sphereBuffer);
                _computeShader.SetInt("SphereCount", sphereColliders.Length);
            }
            else
            {
                _computeShader.SetBuffer(_collisionKernel, "Spheres", _sentinelSphereColliderComputeBuffer);
                _computeShader.SetInt("SphereCount", 0);
            }

            _computeShader.SetFloat("DeltaTime", deltaTime / _physicsWorldConfiguration.DeltaTimeDivisor);

            void DispatchKernel(int kernel)
            {
                _springNodeComputeBuffer.SetData(_springNodes);
                _computeShader.Dispatch(kernel, (_springNodes.Length + NumThreads - 1) / NumThreads, 1, 1);
                _springNodeOutputComputeBuffer.GetData(_springNodeOutputs);

                // Copy positions from output buffer back to input buffer.
                for (var i = 0; i < _springNodeBones.Count; ++i)
                {
                    _springNodes[i].Position = _springNodeOutputs[i].Position;
                    _springNodes[i].Velocity = _springNodeOutputs[i].Velocity;
                }
            }

            for (var t = 0; t < _physicsWorldConfiguration.DeltaTimeDivisor; ++t) DispatchKernel(_forceKernel);

            DispatchKernel(_collisionKernel);

            SyncPositionUpdatesImmediately();

            sphereBuffer?.Dispose();
            cuboidBuffer?.Dispose();
        }

        /// <inheritdoc cref="IClothSpringProcessor.EnumerateNearbySphere"/>
        public IEnumerable<ISpringNode> EnumerateNearbySphere(Vector3 centroid, float radius)
        {
            var sqrRadius = radius * radius;
            for (var i = 0; i < _springNodes.Length; ++i)
            {
                var difference = _springNodeBones[i].transform.localPosition - centroid;
                if (difference.sqrMagnitude <= sqrRadius) yield return new GpuSpringNodeProxy(this, i);
            }
        }

        /// <inheritdoc cref="IClothSpringProcessor.SyncPositionUpdatesImmediately"/>
        public void SyncPositionUpdatesImmediately()
        {
            // Copy positions from buffer back to Unity.
            for (var i = 0; i < _springNodeBones.Count; ++i)
                _springNodeBones[i].transform.position = _springNodes[i].Position;
        }

        /// <inheritdoc cref="IClothSpringProcessor.ResetToInitialState"/>
        public void ResetToInitialState()
        {
            for (var i = 0; i < _springNodes.Length; ++i) _springNodes[i].SnapTo(_initialPositions[i], false);
        }

        /// <inheritdoc cref="IClothSpringProcessor.OnDrawGizmos"/>
        public void OnDrawGizmos()
        {
            foreach (var springNode in _springNodes)
                for (var springDamperId = springNode.SpringDampers.Start;
                    springDamperId < springNode.SpringDampers.End;
                    ++springDamperId)
                {
                    var otherSpringNodeId = _springDampers[(int) springDamperId].OtherNodeId;
                    var otherSpringNode = _springNodes[otherSpringNodeId];
                    Gizmos.DrawLine(springNode.Position, otherSpringNode.Position);
                }
        }
    }
}