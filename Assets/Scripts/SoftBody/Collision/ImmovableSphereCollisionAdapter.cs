using System.Runtime.InteropServices;
using UnityEngine;

namespace SoftBody.Collision
{
    /// <summary>
    /// Adapter for Unity sphere objects that should have no impact on them due to collisions.
    /// They act as if they are immovable.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ImmovableSphereCollisionAdapter : ISphereCollisionResponder
    {
        public Vector3 Position { get; }
        public Vector3 Velocity => Vector3.zero;
        public float Radius { get; }
        public float InverseMass => 0f;

        public ImmovableSphereCollisionAdapter(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }
        
        public ImmovableSphereCollisionAdapter(SphereCollider sphere)
        {
            var transform = sphere.transform;
            Position = sphere.center + transform.position;
            Radius = sphere.radius * transform.localScale.x;
        }

        /// <inheritdoc cref="ICuboidCollisionResponder.ApplyCollisionEffect"/>
        public void ApplyCollisionEffect(Vector3 translation, Vector3 impulse)
        {
            // Do nothing.
        }
    }
}