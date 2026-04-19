using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace VMCBlendShapeControl.Models
{
    public class VmcBlendShapeCatalog
    {
        private readonly ConcurrentDictionary<string, byte> _names = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

        public DateTime LastUpdateUtc { get; private set; } = DateTime.MinValue;

        public int Count => _names.Count;

        public void Add(string blendShapeName)
        {
            if (string.IsNullOrWhiteSpace(blendShapeName))
            {
                return;
            }

            if (_names.TryAdd(blendShapeName.Trim(), 0))
            {
                LastUpdateUtc = DateTime.UtcNow;
            }
        }

        public void Clear()
        {
            _names.Clear();
            LastUpdateUtc = DateTime.UtcNow;
        }

        public IReadOnlyList<string> GetAll()
        {
            return _names.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
