using UnityEngine;

namespace SoftBody.Cpu.Integration
{
    /// <summary>
    /// Explicit euler integration
    /// </summary>
    public sealed class ExplicitEulerIntegrator : IIntegrator
    {
        /// <inheritdoc cref="IIntegrator.Integrate"/>
        public IntegrationEquationVariables Integrate(Vector3 acceleration, float deltaTime,
            IntegrationEquationVariables oldParameters)
        {
            var newPosition = oldParameters.Position + oldParameters.Velocity * deltaTime;
            var newVelocity = oldParameters.Velocity + acceleration * deltaTime;
            return new IntegrationEquationVariables(newVelocity, newPosition);
        }

        /// <inheritdoc cref="IIntegrator.ResetInternalState"/>
        public void ResetInternalState()
        {
            // Do nothing intentionally: no state.
        }
    }
}