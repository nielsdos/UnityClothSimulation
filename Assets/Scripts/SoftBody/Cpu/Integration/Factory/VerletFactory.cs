namespace SoftBody.Cpu.Integration.Factory
{
    /// <summary>
    /// Factory class for creating a Verlet Integrator
    /// </summary>
    public class VerletFactory : IIntegratorFactory
    {
        public IIntegrator Create()
        {
            return new VerletIntegrator();
        }
    }
}