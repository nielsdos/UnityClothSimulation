namespace SoftBody.Cpu.Integration.Factory
{
    /// <summary>
    /// Base interface for an integrator factory
    /// </summary>
    public interface IIntegratorFactory
    {
        /// <summary>
        /// Creates a new integrator of the wanted type.
        /// </summary>
        /// <returns>The integrator instance</returns>
        IIntegrator Create();
    }
}