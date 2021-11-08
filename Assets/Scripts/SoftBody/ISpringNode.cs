using UnityEngine;

namespace SoftBody
{
    /// <summary>
    /// Depending on the implementation of spring node, we can't directly give an instance because it might
    /// be a value type, which would void all changes.
    /// This provides a generic interface to manage the position of the spring node.
    /// </summary>
    public interface ISpringNode
    {
        Vector3 Position { get; }
        int ParticleIndex { get; }

        /// <summary>
        /// Snap node to a point.
        /// Its velocity will be reset.
        /// </summary>
        /// <param name="newPosition">The new position to snap to.</param>
        /// <param name="updateVelocity">Whether velocity should reflect this update.</param>
        void SnapTo(Vector3 newPosition, bool updateVelocity);
    }
}