using System.Runtime.InteropServices;

namespace SoftBody.Gpu
{
    /// <summary>
    /// This struct represents the start and end indices of a part on an array.
    /// Since we can't have pointers on the Gpu, we resort to using big arrays
    /// where we refer to parts of them using this Slice structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Slice
    {
        public readonly uint Start, End;

        /// <summary>
        /// Creates a new slice: [start, end[.
        /// </summary>
        /// <param name="start">The starting index of the slice, inclusive.</param>
        /// <param name="end">The ending index of the slice, non-inclusive.</param>
        public Slice(uint start, uint end)
        {
            Start = start;
            End = end;
        }
    }
}