using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace DataComponents
{
    public class ChunkGrid : IDisposable
    {
        public Dictionary<Vector2, Chunk> ChunkMap;

        public ChunkGrid()
        {
            ChunkMap = new Dictionary<Vector2, Chunk>();
        }

        public void Dispose()
        {
            foreach (var chunk in ChunkMap.Values)
            {
                chunk.Dispose();
            }
            ChunkMap.Clear();
        }
    }
}