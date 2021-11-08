using Configuration;

namespace SoftBody.Cpu.Integration.Factory
{
    /// <summary>
    /// Factory provider for IntegratorFactory objects
    /// </summary>
    public static class IntegratorFactoryProvider
    {
        /// <summary>
        /// Provides a factory based on a chosen integration type
        /// </summary>
        /// <param name="type">The IntegrationType for the needed factory</param>
        /// <returns>An IntegratorFactory</returns>
        public static IIntegratorFactory GetFactoryFor(IntegrationType type)
        {
            return type switch
            {
                IntegrationType.ExplicitEuler => new ExplicitEulerFactory(),
                IntegrationType.RungeKutta4 => new Rk4Factory(),
                IntegrationType.Verlet => new VerletFactory(),
                _ => null
            };
        }
    }
}