using Configuration;
using SoftBody.Collision;
using SoftBody.Cpu.Integration;
using SoftBody.DataStructures;
using UnityEngine;
using Utility;

namespace SoftBody.Cpu
{
    /// <summary>
    /// Represents a spring node.
    /// This can be influenced by the forces of the spring dampers.
    /// </summary>
    public sealed class SpringNode : ISphereCollisionResponder, ISpatialHashable<SpringNode>, ISpringNode
    {
        /*
         * Sources:
         *
         * https://en.wikipedia.org/wiki/Coefficient_of_restitution
         * https://en.wikipedia.org/wiki/Friction#Coefficient_of_friction
         * http://www.darwin3d.com/gdm1999.htm#gdm0599, http://www.darwin3d.com/gamedev/articles/col0599.pdf
         */

        public Vector3 ForceForCurrentStep { get; set; }
        private readonly IIntegrator _integrator;
        private readonly Vector3 _startPosition;

        private readonly Transform _bone;

        // Cache these locally to avoid engine roundtrip to native code.
        public Vector3 Position { get; private set; }
        public int ParticleIndex { get; }

        public Vector3 Velocity { get; private set; }

        public float Radius { get; }
        public float InverseMass => _physicsWorldConfiguration.SpringInverseMass;

        public Vector3 Centroid => _bone.localPosition;
        public Vector3 Size => new Vector3(Radius, Radius, Radius);
        public SpatialHashingItemTracker<SpringNode> ShItemTracker { get; }

        private readonly PhysicsWorldConfiguration _physicsWorldConfiguration;

        /// <param name="correspondingBoneTransform">What bone does this node correspond to.</param>
        /// <param name="integrator">What integration algorithm will be used.</param>
        /// <param name="radius">Radius of the particle sphere.</param>
        /// <param name="physicsWorldConfiguration">The physics world configuration.</param>
        /// <param name="index">This particle index.</param>
        public SpringNode(Transform correspondingBoneTransform, IIntegrator integrator, float radius,
            PhysicsWorldConfiguration physicsWorldConfiguration, int index)
        {
            Radius = radius;
            _physicsWorldConfiguration = physicsWorldConfiguration;
            _bone = correspondingBoneTransform;
            Position = _bone.position;
            _startPosition = Position;
            _integrator = integrator;
            ShItemTracker = new SpatialHashingItemTracker<SpringNode>(this);
            ParticleIndex = index;
        }

        /// <inheritdoc cref="ISpringNode.SnapTo"/>
        public void SnapTo(Vector3 newPosition, bool updateVelocity)
        {
            // Change velocity to reduce twitching.
            if (updateVelocity)
            {
                Velocity = (newPosition - Position) / Time.fixedDeltaTime;
            }
            else
            {
                Velocity = Vector3.zero;
                _bone.position = Position = newPosition;
            }
        }

        /// <inheritdoc cref="ISphereCollisionResponder.ApplyCollisionEffect"/>
        public void ApplyCollisionEffect(Vector3 translation, Vector3 impulse)
        {
            Position += translation * InverseMass;
            Velocity += impulse * InverseMass;
        }

        /// <summary>
        /// Respond to a possible cuboid collision.
        /// </summary>
        /// <param name="other">The cuboid.</param>
        public void RespondToPossibleCollision(ICuboidCollisionResponder other)
        {
            // Closest is the closest point on the cuboid to the current position.
            var closest = Position.Clamp(other.Minimum, other.Maximum);
            var delta = Position - closest;
            var deltaSqrMagnitude = delta.sqrMagnitude;

            if (deltaSqrMagnitude > Radius * Radius)
                return;

            var deltaMagnitude = Mathf.Sqrt(deltaSqrMagnitude);
            var direction = delta / deltaMagnitude;

            var minimumTranslationToResolve = direction * (Radius - deltaMagnitude);
            ApplyCollisionResponse(other, minimumTranslationToResolve, direction);
        }

        /// <summary>
        /// Respond to a possible sphere collision.
        /// </summary>
        /// <param name="other">The sphere.</param>
        public void RespondToPossibleCollision(ISphereCollisionResponder other)
        {
            var directionWithLength = Position - other.Position;
            var lengthSqr = directionWithLength.sqrMagnitude;
            var totalRadius = other.Radius + Radius;
            if (lengthSqr < totalRadius * totalRadius)
            {
                var length = Mathf.Sqrt(lengthSqr);
                var direction = directionWithLength / length;
                var minimumTranslationToResolve = direction * (totalRadius - length);
                ApplyCollisionResponse(other, minimumTranslationToResolve, direction);
            }
        }

        /// <summary>
        /// Applies the collision response.
        /// </summary>
        /// <param name="other">The collider.</param>
        /// <param name="minimumTranslationToResolve">The minimum translation required to move outside of collision.</param>
        /// <param name="direction">The direction vector of the impulse.</param>
        private void ApplyCollisionResponse(ICollisionResponder other, Vector3 minimumTranslationToResolve,
            Vector3 direction)
        {
            var sumOfInverseMasses = InverseMass + other.InverseMass;
            var relativeVelocity = Velocity - other.Velocity;
            var vDotDir = Vector3.Dot(relativeVelocity, direction);

            // Only apply if at least one of the objects is moving towards the other.
            if (vDotDir <= 0f)
            {
                // Calculate the translation and velocity impulse impact.
                var impulseLength = -(1f + _physicsWorldConfiguration.RestitutionConstant) *
                                    (vDotDir / sumOfInverseMasses);
                var impulse = direction * impulseLength;
                minimumTranslationToResolve /= sumOfInverseMasses;

                // Friction
                var relativeVelocityProjected = vDotDir * direction;
                var friction = (relativeVelocity - relativeVelocityProjected) *
                               (_physicsWorldConfiguration.FrictionConstant /
                                sumOfInverseMasses);
                impulse -= friction;

                ApplyCollisionEffect(minimumTranslationToResolve, impulse);
                other.ApplyCollisionEffect(-minimumTranslationToResolve, -impulse);
            }
        }

        /// <summary>
        /// Reset the force for the new time step.
        /// This means all old forces are gone and we start from a clean slate.
        /// The only force applied to ourselves initially is the gravity force.
        /// </summary>
        private void ResetForceForNewStep()
        {
            // Forget the old force.
            ForceForCurrentStep = Vector3.zero;
        }

        /// <summary>
        /// Applies force for the current time step.
        /// New force additions after this are for the next time step, not the current one.
        /// </summary>
        /// <param name="dt">Delta time in time step</param>
        public void ApplyForce(float dt)
        {
            var acceleration =
                ForceForCurrentStep * InverseMass + _physicsWorldConfiguration.Gravity;
            var integrationResult = _integrator.Integrate(
                acceleration,
                dt,
                new IntegrationEquationVariables(Position, Velocity)
            );
            Velocity = integrationResult.Velocity;
            _bone.position = Position = integrationResult.Position;
            ResetForceForNewStep();
        }

        /// <summary>
        /// Resets a springNode to its initial start position
        /// </summary>
        public void ResetToInitialState()
        {
            SnapTo(_startPosition, false);
            ResetForceForNewStep();
            _integrator.ResetInternalState();
        }
    }
}