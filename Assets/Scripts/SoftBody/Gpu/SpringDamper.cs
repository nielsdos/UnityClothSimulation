using System.Runtime.InteropServices;

namespace SoftBody.Gpu
{
    /// <summary>
    /// Represents a spring damper connection between two spring nodes.
    /// Since a Gpu can't use pointers, we give every spring node a unique index which we refer to here.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct SpringDamper
    {
        public readonly uint OtherNodeId;
        public readonly float DesiredDistance;
        public readonly float SpringConstant;

        /// <summary>
        /// Creates a new SpringDamper.
        /// </summary>
        /// <param name="otherNodeId">The node this spring is attached to from the other nodes perspective.</param>
        /// <param name="desiredDistance">The desired distance this spring is in when it's in rest.</param>
        /// <param name="springConstant">The spring constant.</param>
        public SpringDamper(uint otherNodeId, float desiredDistance, float springConstant)
        {
            OtherNodeId = otherNodeId;
            DesiredDistance = desiredDistance;
            SpringConstant = springConstant;
        }
    }
}