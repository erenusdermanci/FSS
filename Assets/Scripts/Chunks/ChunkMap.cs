using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Utils;

namespace Chunks
{
    public class ChunkMap
    {
        public readonly ConcurrentDictionary<Vector2i, Chunk> Map = new ConcurrentDictionary<Vector2i, Chunk>();

        [CanBeNull]
        public Chunk this[Vector2i i] => Map.ContainsKey(i) ? Map[i] : null;

        public void Clear()
        {
            Map.Clear();
        }

        public bool Contains(Vector2i position)
        {
            return Map.ContainsKey(position);
        }

        public void Add(Chunk chunk)
        {
            Map.TryAdd(chunk.Position, chunk);
        }

        public void Remove(Vector2i position)
        {
            Map.TryRemove(position, out _);
        }

        public IEnumerable<Chunk> Chunks()
        {
            return Map.Values;
        }
    }
}