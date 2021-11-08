using UnityEngine;

namespace SoftBody.Collision
{
    /// <summary>
    /// Base interface for handling collision responses.
    /// </summary>
    public interface ICollisionResponder
    {
        Vector3 Velocity { get; }
        float InverseMass { get; }

        /// <summary>
        /// Applies collision effect: translation & impulse
        /// </summary>
        /// <param name="translation">Minimum translation to move outside of collision.</param>
        /// <param name="impulse">The impulse the collision creates.</param>
        void ApplyCollisionEffect(Vector3 translation, Vector3 impulse);
    }
}