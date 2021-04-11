using System.Collections.Generic;
using JetBrains.Annotations;
using Utils;

namespace Chunks
{
    public class ChunkMap<T> where T : Chunk
    {
        private readonly Dictionary<Vector2i, T> _map = new Dictionary<Vector2i, T>();

        [CanBeNull]
        public T this[Vector2i i] => _map.ContainsKey(i) ? _map[i] : null;

        public void Clear()
        {
            _map.Clear();
        }

        public bool Contains(Vector2i position)
        {
            return _map.ContainsKey(position);
        }

        public void Add(T chunk)
        {
            _map.Add(chunk.Position, chunk);
        }

        public void Remove(Vector2i position)
        {
            _map.Remove(position);
        }

        public IEnumerable<T> Chunks()
        {
            return _map.Values;
        }
    }
}