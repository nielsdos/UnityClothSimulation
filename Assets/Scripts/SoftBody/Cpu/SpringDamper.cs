using UnityEngine;

namespace SoftBody.Cpu
{
    /// <summary>
    /// A SpringDamper is a connection between two nodes that emulates a spring with certain properties.
    /// </summary>
    public sealed class SpringDamper
    {
        private readonly SpringNode _firstNodeAttachment, _secondNodeAttachment;
        private readonly float _desiredDistance;
        private readonly float _springConstant;
        private readonly float _springDamping;

        /// <param name="firstNodeAttachment">First node this spring is attached to.</param>
        /// <param name="secondNodeAttachment">Second node this spring is attached to.</param>
        /// <param name="desiredDistance">The desired distance this spring is in when it's in rest.</param>
        /// <param name="springConstant">The spring constant determines the stiffness of the spring.</param>
        /// <param name="springDamping">The amount of dampening according to the relative velocity on the spring force.</param>
        public SpringDamper(SpringNode firstNodeAttachment, SpringNode secondNodeAttachment,
            float desiredDistance, float springConstant, float springDamping)
        {
            _firstNodeAttachment = firstNodeAttachment;
            _secondNodeAttachment = secondNodeAttachment;
            _desiredDistance = desiredDistance;
            _springConstant = springConstant;
            _springDamping = springDamping;
        }

        /// <summary>
        /// Gets the attached nodes as a tuple.
        /// This is only useful for debugging.
        /// </summary>
        /// <returns>The attached nodes.</returns>
        public (SpringNode, SpringNode) GetAttachedNodes()
        {
            return (_firstNodeAttachment, _secondNodeAttachment);
        }

        /// <summary>
        /// Calculates the force this spring applies on its connections.
        /// </summary>
        /// <returns>The spring force</returns>
        private Vector3 CalculateSpringForce()
        {
            var directionWithLength = _secondNodeAttachment.Position - _firstNodeAttachment.Position;
            var currentDistance = directionWithLength.magnitude;
            var deltaDistance = currentDistance - _desiredDistance;
            var directionNormalized = directionWithLength / currentDistance;
            var relativeVelocity = _secondNodeAttachment.Velocity - _firstNodeAttachment.Velocity;
            // Projects the relative velocity to the direction of the spring connection.
            var dampingDirection = Vector3.Dot(relativeVelocity, directionNormalized) *
                                   directionNormalized;
            return _springConstant * deltaDistance * directionNormalized + _springDamping * dampingDirection;
        }

        /// <summary>
        /// Applies spring force to its two connected nodes.
        /// </summary>
        public void ApplySpringForce()
        {
            var force = CalculateSpringForce();
            _firstNodeAttachment.ForceForCurrentStep += force;
            _secondNodeAttachment.ForceForCurrentStep -= force;
        }
    }
}