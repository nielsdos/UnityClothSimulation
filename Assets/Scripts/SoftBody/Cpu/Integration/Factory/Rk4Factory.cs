namespace SoftBody.Cpu.Integration.Factory
{
    /// <summary>
    /// Factory class for creating an RK4 Integrator
    /// </summary>
    public class Rk4Factory : IIntegratorFactory
    {
        private readonly IIntegrator _integrator = new Rk4Integrator();

        public IIntegrator Create()
        {
            return _integrator;
        }
    }
}