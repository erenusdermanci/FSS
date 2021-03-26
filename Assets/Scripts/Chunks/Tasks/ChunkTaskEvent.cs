using System;

namespace Chunks.Tasks
{
    public class ChunkTaskEvent<T> : EventArgs where T : Chunk
    {
        public T Chunk { get; }

        public ChunkTaskEvent(T chunk)
        {
            Chunk = chunk;
        }
    }
}
