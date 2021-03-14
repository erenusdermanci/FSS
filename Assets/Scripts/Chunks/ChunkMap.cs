using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Chunks
{
    public class ChunkMap
    {
        public readonly ConcurrentDictionary<Vector2, Chunk> Map = new ConcurrentDictionary<Vector2, Chunk>();

        [CanBeNull]
        public Chunk this[Vector2 i] => Map.ContainsKey(i) ? Map[i] : null;

        public void Clear()
        {
            Map.Clear();
        }

        public bool Contains(Vector2 position)
        {
            return Map.ContainsKey(position);
        }

        public void Add(Chunk chunk)
        {
            Map.TryAdd(chunk.Position, chunk);
        }

        public void Remove(Vector2 position)
        {
            Map.TryRemove(position, out _);
        }

        public IEnumerable<Chunk> Chunks()
        {
            return Map.Values;
        }
    }
}