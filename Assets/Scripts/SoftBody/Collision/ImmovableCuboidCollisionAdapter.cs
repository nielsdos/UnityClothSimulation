using System.Runtime.InteropServices;
using UnityEngine;

namespace SoftBody.Collision
{
    /// <summary>
    /// Adapter for Unity cuboid objects that should have no impact on them due to collisions.
    /// They act as if they are immovable.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ImmovableCuboidCollisionAdapter : ICuboidCollisionResponder
    {
        public Vector3 Velocity => Vector3.zero;
        public Vector3 Minimum { get; }
        public Vector3 Maximum { get; }
        public float InverseMass => 0f;

        public ImmovableCuboidCollisionAdapter(Vector3 centerPosition, Vector3 size)
        {
            size /= 2f;
            Minimum = centerPosition - size;
            Maximum = centerPosition + size;
        }

        public ImmovableCuboidCollisionAdapter(BoxCollider cuboid)
        {
            var transform = cuboid.transform;
            var size = Vector3.Scale(cuboid.size * 0.5f, transform.localScale);
            var position = cuboid.center + transform.position;
            Minimum = position - size;
            Maximum = position + size;
        }

        /// <inheritdoc cref="ICuboidCollisionResponder.ApplyCollisionEffect"/>
        public void ApplyCollisionEffect(Vector3 translation, Vector3 impulse)
        {
            // Do nothing.
        }
    }
}