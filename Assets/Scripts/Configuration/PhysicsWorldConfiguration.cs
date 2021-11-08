using System.ComponentModel;
using SoftBody;
using UnityEngine;

namespace Configuration
{
    public sealed class PhysicsWorldConfiguration
    {
        public uint DeltaTimeDivisor { get; set; }
        public float GravityMultiplier { get; set; }
        public IntegrationType IntegrationType { get; set; }
        public float ElasticSpringConstant { get; set; }
        public float ShearSpringConstant { get; set; }
        public float BendSpringConstant { get; set; }
        public float MeshBasedElasticSpringConstant { get; set; }
        public float MeshBasedShearSpringConstant { get; set; }
        public float SpringInverseMass { get; set; }
        public float SpringDamping { get; set; }

        /// <summary>
        /// The ratio of the final to initial relative velocity between two objects after collision.
        /// </summary>
        public float RestitutionConstant { get; set; }

        public float FrictionConstant { get; set; }
        public Vector3 Gravity => Physics.clothGravity * GravityMultiplier;

        /// <summary>
        /// Gets the spring constant for the given spring damper type.
        /// </summary>
        /// <param name="type">The spring damper type.</param>
        /// <returns>The spring constant.</returns>
        /// <exception cref="InvalidEnumArgumentException">An invalid spring damper type was given. This should never happen and means there is a missing case here.</exception>
        public float SpringConstantForType(SpringDamperType type)
        {
            return type switch
            {
                SpringDamperType.Elastic => ElasticSpringConstant,
                SpringDamperType.Shear => ShearSpringConstant,
                SpringDamperType.Bend => BendSpringConstant,
                SpringDamperType.MeshElastic => MeshBasedElasticSpringConstant,
                SpringDamperType.MeshShear => MeshBasedShearSpringConstant,
                _ => throw new InvalidEnumArgumentException()
            };
        }
    }
}