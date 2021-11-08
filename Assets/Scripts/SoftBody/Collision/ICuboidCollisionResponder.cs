using UnityEngine;

namespace SoftBody.Collision
{
    /// <summary>
    /// Base interface for cuboids that handle collision responses.
    /// </summary>
    public interface ICuboidCollisionResponder : ICollisionResponder
    {
        Vector3 Minimum { get; }
        Vector3 Maximum { get; }
    }
}