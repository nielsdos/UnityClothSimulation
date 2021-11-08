namespace SoftBody.Cpu.Integration.Factory
{
    /// <summary>
    /// Factory class for creating an Explicit Euler Integrator
    /// </summary>
    public class ExplicitEulerFactory : IIntegratorFactory
    {
        private readonly IIntegrator _integrator = new ExplicitEulerIntegrator();

        public IIntegrator Create()
        {
            return _integrator;
        }
    }
}