using UnityEngine;

namespace SoftBody.Cpu.Integration
{
    /// <summary>
    /// RK4 integration
    /// </summary>
    public sealed class Rk4Integrator : IIntegrator
    {
        /**
         * Sources: https://en.wikipedia.org/wiki/Runge%E2%80%93Kutta_methods
         */
        /// <inheritdoc cref="IIntegrator.Integrate"/>
        public IntegrationEquationVariables Integrate(Vector3 acceleration, float deltaTime,
            IntegrationEquationVariables state)
        {
            // This is the function f = dy/dt that is used in the RK4 description.
            // Note that this function is independent of the time, hence no time parameter is provided.
            IntegrationEquationVariables DyDt(IntegrationEquationVariables system)
            {
                // x' = v
                // v' = acceleration
                return new IntegrationEquationVariables(system.Velocity, acceleration);
            }

            var k1 = DyDt(state);
            var k2 = DyDt(state + k1 * (deltaTime / 2.0f));
            var k3 = DyDt(state + k2 * (deltaTime / 2.0f));
            var k4 = DyDt(state + k3 * deltaTime);
            state += (k1 + (k2 + k3) * 2.0f + k4) * (deltaTime / 6.0f);

            return new IntegrationEquationVariables(state.Position, state.Velocity);
        }

        /// <inheritdoc cref="IIntegrator.ResetInternalState"/>
        public void ResetInternalState()
        {
            // Do nothing intentionally: no state.
        }
    }
}