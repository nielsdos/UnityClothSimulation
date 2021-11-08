using System;
using SoftBody;
using UnityEngine;

namespace Utility
{
    /// <summary>
    /// Component grabbing the cloth particles "magnetically".
    /// The particles are moved to this position relative to their own starting position.
    /// </summary>
    public sealed class SpringNodeGrabbed : MonoBehaviour
    {
        public ISpringNode[] SpringNodes;
        [NonSerialized] public bool RotateTowardsOrientation;
        private Vector3[] _offsets;
        private Vector3 _startPosition;

        private void Start()
        {
            _offsets = new Vector3[SpringNodes.Length];
            _startPosition = transform.position;

            for (var i = 0; i < SpringNodes.Length; ++i)
                _offsets[i] = SpringNodes[i].Position - _startPosition;
        }

        /// <summary>
        /// Calculates the rotation of the particles such that they are rotated towards the movement of the magnet.
        /// </summary>
        /// <returns>The rotation.</returns>
        private Quaternion CalculateRotationOfClothParticles()
        {
            var rotation = Quaternion.identity;
            if (!RotateTowardsOrientation)
                return rotation;
            var forwardLookDirection = (transform.position - _startPosition).normalized;
            if (forwardLookDirection != Vector3.zero)
                rotation = Quaternion.Euler(90f, 0, 0) * Quaternion.LookRotation(forwardLookDirection, Vector3.up);
            return rotation;
        }

        /// <summary>
        /// Moves the particles by applying the position and velocity updates and setting the rotation.
        /// </summary>
        /// <param name="rotation">The rotation of the particles.</param>
        private void ApplyForcesAndRotationToClothParticles(Quaternion rotation)
        {
            transform.localRotation = rotation;

            for (var i = 0; i < SpringNodes.Length; ++i)
                SpringNodes[i].SnapTo(transform.position + rotation * _offsets[i], true);
        }

        private void FixedUpdate()
        {
            ApplyForcesAndRotationToClothParticles(CalculateRotationOfClothParticles());
        }
    }
}