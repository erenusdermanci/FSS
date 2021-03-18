using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using Utils;

namespace Chunks
{
    public class ChunkMap<T> where T : Chunk
    {
        public readonly ConcurrentDictionary<Vector2i, T> Map = new ConcurrentDictionary<Vector2i, T>();

        [CanBeNull]
        public T this[Vector2i i] => Map.ContainsKey(i) ? Map[i] : null;

        public void Clear()
        {
            Map.Clear();
        }

        public bool Contains(Vector2i position)
        {
            return Map.ContainsKey(position);
        }

        public void Add(T chunk)
        {
            Map.TryAdd(chunk.Position, chunk);
        }

        public void Remove(Vector2i position)
        {
            Map.TryRemove(position, out _);
        }

        public IEnumerable<T> Chunks()
        {
            return Map.Values;
        }
    }
}