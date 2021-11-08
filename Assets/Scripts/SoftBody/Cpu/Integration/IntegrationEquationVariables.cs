using UnityEngine;

namespace SoftBody.Cpu.Integration
{
    /// <summary>
    /// Represents the system of ODE 1st order equations used as input and result of the integrators.
    /// </summary>
    public struct IntegrationEquationVariables
    {
        public Vector3 Position;
        public Vector3 Velocity;

        public IntegrationEquationVariables(Vector3 position, Vector3 velocity)
        {
            Position = position;
            Velocity = velocity;
        }

        public static IntegrationEquationVariables operator +(IntegrationEquationVariables a,
            IntegrationEquationVariables b)
        {
            return new IntegrationEquationVariables(a.Position + b.Position, a.Velocity + b.Velocity);
        }

        public static IntegrationEquationVariables operator *(IntegrationEquationVariables a, float b)
        {
            return new IntegrationEquationVariables(a.Position * b, a.Velocity * b);
        }
    }
}