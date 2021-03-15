using System;

namespace Chunks.Tasks
{
    public class ChunkTaskEvent : EventArgs
    {
        public Chunk Chunk { get; }

        public ChunkTaskEvent(Chunk chunk)
        {
            Chunk = chunk;
        }
    }
}