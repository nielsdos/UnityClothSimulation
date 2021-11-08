using UnityEngine;

namespace Utility
{
    /// <summary>
    /// A recorded position and scale, used in <see cref="RecordPosition"/> and <see cref="ReplayPosition"/>.
    /// </summary>
    public struct RecordedPositionAndScale
    {
        public Vector3 Position, Scale;

        public RecordedPositionAndScale(Vector3 position, Vector3 scale)
        {
            Position = position;
            Scale = scale;
        }
    }
}