using UnityEngine;

namespace SoftBody.Collision
{
    /// <summary>
    /// Base interface for spheres that handle collision responses.
    /// </summary>
    public interface ISphereCollisionResponder : ICollisionResponder
    {
        Vector3 Position { get; }
        float Radius { get; }
    }
}