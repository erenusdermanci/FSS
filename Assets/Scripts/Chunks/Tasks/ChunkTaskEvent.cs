using System;

namespace Chunks.Tasks
{
    public class ChunkTaskEvent : EventArgs
    {
        public ChunkServer Chunk { get; }

        public ChunkTaskEvent(ChunkServer chunk)
        {
            Chunk = chunk;
        }
    }
}