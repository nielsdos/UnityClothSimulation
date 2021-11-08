namespace RigidBody
{
    /// <summary>
    /// Baxter gripper that will automatically attach to the closest attachable point.
    /// </summary>
    public class AutomaticBaxterHandGrab : BaxterHandGrab
    {
        private void FixedUpdate()
        {
            if (!IsAttached && isOn) AttachToClosest();
        }
    }
}