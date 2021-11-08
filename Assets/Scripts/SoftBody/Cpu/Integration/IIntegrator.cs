using UnityEngine;

namespace SoftBody.Cpu.Integration
{
    /// <summary>
    /// Base interface for integrators
    /// </summary>
    public interface IIntegrator
    {
        /// <summary>
        /// Integrates according to the current acceleration, time since last integration, the old position and velocity.
        /// </summary>
        /// <param name="acceleration">The new acceleration.</param>
        /// <param name="deltaTime">The delta time over which to integrate.</param>
        /// <param name="oldParameters">The parameters from previous iteration.</param>
        /// <returns>The new position and velocity.</returns>
        IntegrationEquationVariables Integrate(Vector3 acceleration, float deltaTime,
            IntegrationEquationVariables oldParameters);

        /// <summary>
        /// Resets the internal integration state to a resting start state.
        /// </summary>
        void ResetInternalState();
    }
}