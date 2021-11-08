using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Utility
{
    /// <summary>
    /// This component replays the positions that are stored in a text file.
    /// The purpose is to use this in standalone demos to replay positional changes made in the editor.
    /// </summary>
    public sealed class ReplayPositionAndScale : MonoBehaviour
    {
        [SerializeField] private string fileName;
        private List<RecordedPositionAndScale> _positions;
        private int _currentReplayIndex;

        private void Awake()
        {
            _positions = new List<RecordedPositionAndScale>();

            var lines = File.ReadAllLines(fileName);
            foreach (var line in lines)
            {
                var split = line.Split(' ');

                Vector3 ReadVector(int offset)
                {
                    var x = float.Parse(split[offset + 0], CultureInfo.InvariantCulture.NumberFormat);
                    var y = float.Parse(split[offset + 1], CultureInfo.InvariantCulture.NumberFormat);
                    var z = float.Parse(split[offset + 2], CultureInfo.InvariantCulture.NumberFormat);
                    return new Vector3(x, y, z);
                }

                _positions.Add(new RecordedPositionAndScale(ReadVector(0), ReadVector(3)));
            }
        }

        private void FixedUpdate()
        {
            if (_currentReplayIndex >= _positions.Count) return;
            var rp = _positions[_currentReplayIndex];
            var tr = transform;
            tr.localPosition = rp.Position;
            tr.localScale = rp.Scale;
            ++_currentReplayIndex;
        }
    }
}