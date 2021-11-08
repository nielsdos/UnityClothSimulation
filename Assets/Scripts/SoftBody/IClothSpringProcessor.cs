using System.Collections.Generic;
using SoftBody.Collision;
using Unity.Collections;
using UnityEngine;

namespace SoftBody
{
    /// <summary>
    /// This interface is responsible for structuring the spring nodes and dampers internally and simulating them.
    /// It abstracts away the Gpu or Cpu simulation.
    /// </summary>
    public interface IClothSpringProcessor
    {
        /// <summary>
        /// Gets the spring node for the spring node with the given index.
        /// </summary>
        /// <param name="index">The index of the spring node.</param>
        /// <returns>The spring node.</returns>
        ISpringNode SpringNodeFor(int index);

        /// <summary>
        /// Adds a new spring node. It is modelled as a sphere.
        /// Every spring node corresponds to a bone transform in Unity, which is used to deform the mesh.
        /// </summary>
        /// <param name="correspondingBoneTransform">The bone transform used to deform the mesh.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <returns>The unique index of the newly added spring node. This can be used as input for the spring damper.</returns>
        int AddSpringNode(Transform correspondingBoneTransform, float radius);

        /// <summary>
        /// Adds a new spring damper, connected between two spring nodes.
        /// </summary>
        /// <param name="type">The type of the damper, e.g. elastic, shear, ...</param>
        /// <param name="desiredDistance">The desired distance this spring is in when it's in rest.</param>
        /// <param name="firstNodeIndex">First node this spring is attached to.</param>
        /// <param name="secondNodeIndex">Second node this spring is attached to.</param>
        void AddSpringDamper(SpringDamperType type, float desiredDistance, int firstNodeIndex, int secondNodeIndex);

        /// <summary>
        /// Runs a single step of cloth simulation.
        /// </summary>
        /// <param name="deltaTime">How much time has passed since the last cloth simulation step.</param>
        /// <param name="sphereColliders">An array containing the sphere colliders in the scene we can collide with.</param>
        /// <param name="cuboidColliders">An array containing the cuboid colliders in the scene we can collide with.</param>
        void FixedUpdate(float deltaTime, NativeArray<ImmovableSphereCollisionAdapter> sphereColliders,
            NativeArray<ImmovableCuboidCollisionAdapter> cuboidColliders);

        /// <summary>
        /// Enumerates the objects in this spring processor that are nearby a sphere with the given centroid and radius.
        /// </summary>
        /// <param name="centroid">The centroid of the sphere.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <returns>The nearby objects.</returns>
        IEnumerable<ISpringNode> EnumerateNearbySphere(Vector3 centroid, float radius);

        /// <summary>
        /// Immediately make positional updates available to the other methods.
        /// </summary>
        void SyncPositionUpdatesImmediately();

        /// <summary>
        /// Resets the cloth to the initial starting state it was in before the first update ever happened.
        /// </summary>
        void ResetToInitialState();

        /// <summary>
        /// Unity allows us to draw debug visuals using this method.
        /// </summary>
        void OnDrawGizmos();

        /// <summary>
        /// Explicit initialization that needs to be performed when every node and damper has been added.
        /// </summary>
        void FinishInitialization();

        /// <summary>
        /// Cleanup procedure called when the Unity cloth object is destroyed.
        /// </summary>
        void OnDestroy();
    }
}