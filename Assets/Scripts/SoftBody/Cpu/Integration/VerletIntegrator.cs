using UnityEngine;

namespace SoftBody.Cpu.Integration
{
    /// <summary>
    /// Velocity verlet integrator.
    /// </summary>
    public class VerletIntegrator : IIntegrator
    {
        /*
         * Source: https://en.wikipedia.org/wiki/Verlet_integration#Velocity_Verlet
         */
        private Vector3 _oldAcceleration;

        /// <inheritdoc cref="IIntegrator.Integrate"/>
        public IntegrationEquationVariables Integrate(Vector3 acceleration, float deltaTime,
            IntegrationEquationVariables oldParameters)
        {
            var newPosition = oldParameters.Position + oldParameters.Velocity * deltaTime +
                              _oldAcceleration * (deltaTime * deltaTime * 0.5f);
            var newVelocity = oldParameters.Velocity + (_oldAcceleration + acceleration) * (deltaTime * 0.5f);
            _oldAcceleration = acceleration;
            return new IntegrationEquationVariables(newPosition, newVelocity);
        }

        /// <inheritdoc cref="IIntegrator.ResetInternalState"/>
        public void ResetInternalState()
        {
            _oldAcceleration = Vector3.zero;
        }
    }
}