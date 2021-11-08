using System;
using System.Collections.Generic;
using System.Linq;
using SoftBody;
using UnityEngine;
using UnityEngine.Serialization;
using Utility;

namespace RigidBody
{
    /// <summary>
    /// Adding this component to Baxter's hand will allow the Baxter component to call this script
    /// to add a constraint that attaches this hand to another object.
    /// </summary>
    public class BaxterHandGrab : MonoBehaviour
    {
        [NonSerialized] protected bool IsAttached;
        [FormerlySerializedAs("IsOn")] public bool isOn;
        [SerializeField] private bool dontRotateTowardsOrientation;

        [Tooltip("The radius the hand needs to be within to automatically grab the cloth piece.")] [SerializeField]
        private float checkRadius = 0.5f;

        [Tooltip("Whether you want to grab all the points within the specified checking radius.")] [SerializeField]
        private bool grabMultiple;

        [SerializeField] private ClothSimulation cloth;

        private Vector3 RelativeClothPosition => cloth.transform.InverseTransformPoint(transform.position);

        /// <summary>
        /// Whether the magnet can grab something, i.e. at least one point is in range.
        /// </summary>
        /// <returns>True if the magnet can grab something, false otherwise.</returns>
        public bool CanGrabSomething()
        {
            return cloth.EnumerateNearbySphere(RelativeClothPosition, checkRadius).Any();
        }

        /// <summary>
        /// Whether the grabber contains one of the provided indices.
        /// </summary>
        /// <param name="indices">The indices the grabber can contain to return true.</param>
        /// <returns>True if the index is contained, false otherwise.</returns>
        public bool DoesGrabberContainOneOfIndices(params int[] indices)
        {
            // No LINQ due to overhead constraints.
            foreach (var node in cloth.EnumerateNearbySphere(RelativeClothPosition, checkRadius))
                if (indices.Contains(node.ParticleIndex)) return true;

            return false;
        }

        /// <summary>
        /// Attaches this hand grabber to all the spring nodes in the array.
        /// </summary>
        /// <param name="springsToAttachTo">The spring nodes to attach to.</param>
        public void AttachTo(ISpringNode[] springsToAttachTo)
        {
            var gameObjectGrabber = gameObject.AddComponent<SpringNodeGrabbed>();
            gameObjectGrabber.SpringNodes = springsToAttachTo;
            gameObjectGrabber.RotateTowardsOrientation = !dontRotateTowardsOrientation;
            IsAttached = true;
        }

        /// <summary>
        /// Attaches this hand grabber to the closest spring node(s).
        /// </summary>
        public void AttachToClosest()
        {
            var offset = cloth.transform.position;
            var myPosition = RelativeClothPosition;
            var sqrCheckRadius = checkRadius * checkRadius;

            var minSqrDistance = sqrCheckRadius;
            // Lazily allocate the list.
            List<ISpringNode> springNodesToAttachTo = null;
            foreach (var springNode in cloth.EnumerateNearbySphere(myPosition, checkRadius))
                if (grabMultiple)
                {
                    springNodesToAttachTo ??= new List<ISpringNode>();
                    springNodesToAttachTo.Add(springNode);
                }
                else
                {
                    var difference = springNode.Position - offset - myPosition;
                    var sqrDistance = difference.sqrMagnitude;

                    // Choose particle with smallest distance.
                    if (sqrDistance <= minSqrDistance)
                    {
                        if (ReferenceEquals(springNodesToAttachTo, null))
                            springNodesToAttachTo = new List<ISpringNode>();
                        else
                            springNodesToAttachTo.Clear();

                        springNodesToAttachTo.Add(springNode);
                        minSqrDistance = sqrDistance;
                    }
                }

            if (!ReferenceEquals(springNodesToAttachTo, null))
                AttachTo(springNodesToAttachTo.ToArray());
        }

        /// <summary>
        /// Detaches this hand grabber from all attached spring nodes.
        /// </summary>
        public void Detach()
        {
            Destroy(gameObject.GetComponent<SpringNodeGrabbed>());
            IsAttached = false;
        }
    }
}