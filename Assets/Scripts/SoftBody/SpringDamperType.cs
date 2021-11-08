namespace SoftBody
{
    /// <summary>
    /// Enum with all spring damper types
    /// </summary>
    public enum SpringDamperType
    {
        Elastic,
        Shear,
        Bend,

        // These are tweaked for model based cloths.
        MeshElastic,
        MeshShear
    }
}