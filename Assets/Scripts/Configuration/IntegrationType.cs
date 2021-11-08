using System;

namespace Configuration
{
    [Serializable]
    public enum IntegrationType
    {
        ExplicitEuler,
        RungeKutta4,
        Verlet
    }
}